using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using TnTRFMod.Utils;

namespace TnTRFMod.Patches;

[HarmonyPatch]
internal class LibTaikoPatches
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(nint lpAddress, nuint dwSize,
        uint flNewProtect, out uint lpflOldProtect);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool GetModuleInformation(
        IntPtr hProcess,
        IntPtr hModule,
        out MODULEINFO lpmodinfo,
        uint cb);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, uint nSize);

    private static unsafe void PatchInt32(byte* moduleBase, long rva, int value)
    {
        var target = (nint)(moduleBase + rva);
        VirtualProtect(target, 4u, 0x40u, out var oldProtect); // PAGE_EXECUTE_READWRITE
        *(int*)target = value;
        VirtualProtect(target, 4u, oldProtect, out _);
    }

    private static unsafe uint GetModuleFileCrc32(IntPtr moduleBase)
    {
        var pathBuilder = new StringBuilder(1024);
        var len = GetModuleFileName(moduleBase, pathBuilder, (uint)pathBuilder.Capacity);
        if (len == 0)
        {
            Logger.Error($"GetModuleFileName failed: {Marshal.GetLastWin32Error()}");
            return 0;
        }

        var path = pathBuilder.ToString();
        var bytes = File.ReadAllBytes(path);
        fixed (byte* p = bytes)
        {
            return Crc32.crc32(0, p, bytes.Length);
        }
    }

    internal static void InitExpandCSyousetsu(int N)
    {
        var moduleBase = GetModuleHandle("LibTaiko.dll");
        if (moduleBase == IntPtr.Zero)
        {
            Logger.Error("Can't get handle of LibTaiko.dll, aborting CSyousetsu expansion.");
            return;
        }

        var crc = GetModuleFileCrc32(moduleBase);
        Logger.Info($"LibTaiko.dll CRC32 (file): 0x{crc:X8}");

        if (crc == 0x1E5B3CFF)
            InitExpandCSyousetsu_Ver1E5B3CFF(N);
        else
            Logger.Warn(
                "LibTaiko.dll crc mismatch, maybe it's updated and the mod isn't supported this, aborting CSyousetsu expansion.");
    }

    private static unsafe void InitExpandCSyousetsu_Ver1E5B3CFF(int N)
    {
        const int SzPerEntry = 208; // 每个 CSyousetsu 元素的字节数
        const int MetaExtra = 496; // 数组之后的元数据区域大小（固定）
        const int NumLanes = 5; // FumenMan 内最多有 5 个 CLane（5 难度）
        const int FumenExtra = 8 + 22288; // FumenMan 中除 CLane 数组外的固定开销

        var claneSize = N * SzPerEntry + MetaExtra; // 新 CLane 总大小
        var fumenManSize = FumenExtra + NumLanes * claneSize; // 新 FumenMan 堆分配大小

        // 各元数据字段相对 CLane 基址的新偏移量（数组结束后顺次排列）
        var offSyousetsuCount = N * SzPerEntry + 8; // CLane->syousetsu_count（当前已解析个数）
        var offRawLanePtr = N * SzPerEntry + 16; // CLane->raw_lane_header_ptr（指向谱面二进制头）
        var offTArrayVtable = N * SzPerEntry + 24; // CLane->ISyousetsuData TArray vtable
        var offTArrayBegin = N * SzPerEntry + 32; // CLane->TArray.begin
        var offTArrayCur = N * SzPerEntry + 40; // CLane->TArray.cur
        var offTArrayEnd = N * SzPerEntry + 48; // CLane->TArray.end
        var offCourseCount = N * SzPerEntry + 56; // CLane->course_count
        var offCCourseStart = N * SzPerEntry + 64; // CLane->CCourse 内联数据区域起始

        var moduleBase = (byte*)GetModuleHandle("LibTaiko.dll").ToPointer();
        if (moduleBase == null)
        {
            Logger.Error("Can't get handle of LibTaiko.dll, aborting CSyousetsu expansion.");
            return;
        }

        Logger.Info($"Expanding limit of CSyousetsu from 300 to {N}...");
        Logger.Info($"  CLane size 0x{claneSize:X}  FumenMan size 0x{fumenManSize:X}");

        // 补丁 A/B：CLane 结构体大小
        //   A — CFumen_ParseLanes 中 `imul rcx, rax, CLaneSize` 的立即数
        //       决定了访问各 CLane 时的步长（stride）
        //   B — CLane_Dtor 中 `mov edx, CLaneSize` 传给 sized-free 的大小
        //       必须与 A 完全一致，否则 free 会损坏堆
        PatchInt32(moduleBase, 0x51A4L, claneSize); // A: CFumen_ParseLanes imul 系数
        PatchInt32(moduleBase, 0x66A8L, claneSize); // B: CLane_Dtor sized-free 大小

        // FumenMan 堆分配大小
        //   Initialize_0 中 `std::_Allocate(FumenManSize)` 的参数
        //   公式：8 + 5 * CLaneSize + 22288
        PatchInt32(moduleBase, 0x23BAL, fumenManSize); // C: Initialize_0 堆分配大小

        // CLane->syousetsu_count 字段偏移
        //   旧值 0xF3C8 = 300*208+8，出现在以下所有访问该字段的指令里
        PatchInt32(moduleBase, 0x51BAL, offSyousetsuCount); // CFumen_ParseLanes: 初始化为 0
        PatchInt32(moduleBase, 0x6864L, offSyousetsuCount); // CLane_ParseSyousetsu: 读取当前计数
        PatchInt32(moduleBase, 0x6A23L, offSyousetsuCount); // CLane_ParseSyousetsu: 自增计数

        // CLane->raw_lane_header_ptr 字段偏移
        //   旧值 0xF3D0；该指针指向谱面二进制数据中的 Lane 头，在 ParseLanes
        //   和一批 vtable 访问器函数里被频繁引用（共 11 处）
        PatchInt32(moduleBase, 0x51C1L, offRawLanePtr); // CFumen_ParseLanes: 初始化
        PatchInt32(moduleBase, 0x51EBL, offRawLanePtr); // CFumen_ParseLanes: 读取（取CSyousetsu数量）
        PatchInt32(moduleBase, 0x5248L, offRawLanePtr); // CFumen_ParseLanes: 读取（parse 循环条件）
        PatchInt32(moduleBase, 0x5262L, offRawLanePtr); // CFumen_ParseLanes: 读取（parse 循环体）
        // 以下 7 处位于 CLane vtable 访问器函数体内（每个函数仅 mov rax,[rcx+off]; ret）
        PatchInt32(moduleBase, 0x6733L, offRawLanePtr); // CLane_GetSyousetsuRawFlag vtable func #1
        PatchInt32(moduleBase, 0x6743L, offRawLanePtr); // CLane_GetSyousetsuRawFlag vtable func #2
        PatchInt32(moduleBase, 0x6753L, offRawLanePtr); // CLane_GetSyousetsuRawFlag vtable func #3
        PatchInt32(moduleBase, 0x6763L, offRawLanePtr); // CLane_GetSyousetsuRawFlag vtable func #4
        PatchInt32(moduleBase, 0x6773L, offRawLanePtr); // CLane_GetSyousetsuRawFlag vtable func #5
        PatchInt32(moduleBase, 0x6783L, offRawLanePtr); // CLane_GetSyousetsuRawFlag vtable func #6
        PatchInt32(moduleBase, 0x6793L, offRawLanePtr); // CLane_GetSyousetsuRawFlag vtable func #7

        // CLane->ISyousetsuData TArray vtable 字段偏移
        //   旧值 0xF3D8；出现在初始化、析构、及一个 thunk 函数里（共 4 处）
        //   thunk (0x1F086) 形式为 `add rcx, 0xF3D8`，用于把 this 调整为 TArray 地址
        PatchInt32(moduleBase, 0x51DDL, offTArrayVtable); // CFumen_ParseLanes: 写入 vtable
        // 注意：指令 `mov [rcx+0F3D8h], rax` 的起始地址是 0x180006660（REX+opcode+ModRM 共 3 字节），
        //   32-bit 位移字段从 +3 处开始，对应 RVA = 0x6663，不是 0x6660。
        //   写错偏移会损坏指令本身，导致 CLane_Dtor 析构时崩溃。
        PatchInt32(moduleBase, 0x6663L, offTArrayVtable); // CLane_Dtor: 重置 vtable（位移字段起始 RVA）
        PatchInt32(moduleBase, 0x67A3L, offTArrayVtable); // CLane_GetISyousetsuDataTArrayPtr
        PatchInt32(moduleBase, 0x1F086L, offTArrayVtable); // thunk: `add rcx, offsetOfTArray`（立即数位于 RVA 0x1F086）

        // CLane->TArray.begin 字段偏移
        //   旧值 0xF3E0；TArray 的起始指针，用于遍历/边界检测（共 7 处）
        PatchInt32(moduleBase, 0x51C8L, offTArrayBegin); // CFumen_ParseLanes: 初始化为 0
        PatchInt32(moduleBase, 0x51F5L, offTArrayBegin); // CFumen_ParseLanes: 读取（边界检查）
        PatchInt32(moduleBase, 0x666AL, offTArrayBegin); // CLane_Dtor: 读取
        PatchInt32(moduleBase, 0x6685L, offTArrayBegin); // CLane_Dtor: 写回 0
        PatchInt32(moduleBase, 0x67CCL, offTArrayBegin); // CLane_ClearSyousetsuData: 读取
        PatchInt32(moduleBase, 0x6803L, offTArrayBegin); // CLane_ClearSyousetsuData: 写回 0
        PatchInt32(moduleBase, 0x69FBL, offTArrayBegin); // CLane_ParseSyousetsu: 读取

        // CLane->TArray.cur 字段偏移
        //   旧值 0xF3E8；TArray 的当前写入位置（Push 时更新，共 6 处）
        PatchInt32(moduleBase, 0x51CFL, offTArrayCur); // CFumen_ParseLanes: 初始化为 0
        PatchInt32(moduleBase, 0x6671L, offTArrayCur); // CLane_Dtor: 读取
        PatchInt32(moduleBase, 0x668CL, offTArrayCur); // CLane_Dtor: 写回 0
        PatchInt32(moduleBase, 0x67C2L, offTArrayCur); // CLane_ClearSyousetsuData
        PatchInt32(moduleBase, 0x680FL, offTArrayCur); // CLane_ClearSyousetsuData
        PatchInt32(moduleBase, 0x682BL, offTArrayCur); // CLane_ClearSyousetsuData

        // CLane->TArray.end 字段偏移
        //   旧值 0xF3F0；TArray 的容量上界（共 2 处）
        PatchInt32(moduleBase, 0x51D6L, offTArrayEnd); // CFumen_ParseLanes: 初始化为 0
        PatchInt32(moduleBase, 0x6693L, offTArrayEnd); // CLane_Dtor: 写回 0

        // CLane->course_count 字段偏移
        //   旧值 0xF3F8；记录当前 CLane 已解析的 CCourse 数量（共 3 处）
        PatchInt32(moduleBase, 0x51E4L, offCourseCount); // CFumen_ParseLanes
        PatchInt32(moduleBase, 0x68F5L, offCourseCount); // CLane_ParseSyousetsu: 读取
        PatchInt32(moduleBase, 0x68FFL, offCourseCount); // CLane_ParseSyousetsu: 传参

        // CLane->CCourse 内联数据区域起始偏移
        //   旧值 0xF400；CLane_Initialize 和 CFumen_ParseLanes 用其访问 CCourse 块
        //   CCourse 区域紧接在上面各字段之后，偏移 = N*208 + 64
        PatchInt32(moduleBase, 0x4E32L, offCCourseStart); // CLane_Initialize
        PatchInt32(moduleBase, 0x5214L, offCCourseStart); // CFumen_ParseLanes

        Logger.Info($"Applied all CSyousetsu patches, CSyousetsu limit is now {N}.");
    }

    // ── Win32 API ──────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    public struct MODULEINFO
    {
        public IntPtr lpBaseOfDll;
        public uint SizeOfImage;
        public IntPtr EntryPoint;
    }
}