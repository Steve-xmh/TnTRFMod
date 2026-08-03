using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;

namespace TnTRFMod.Utils.Fumen;

public sealed class FumenReader
{
    private const int JudgeTimingCount = 36;
    private const int JudgeTimingSize = 12;
    private const int HeaderSize = 0x208;
    private const int MeasuresOffset = HeaderSize;

    internal readonly byte[] fumenData;
    private readonly FumenMeasure[] parsedMeasures;

    public FumenReader(byte[] fumenData)
    {
        ArgumentNullException.ThrowIfNull(fumenData);
        EnsureRange(fumenData, 0, HeaderSize, "Fumen V2 header");

        this.fumenData = fumenData;
        var count = checked((int)MeasureCount);
        parsedMeasures = new FumenMeasure[count];

        var readPosition = MeasuresOffset;
        for (var i = 0; i < count; i++)
        {
            try
            {
                var measure = new FumenMeasure(fumenData, readPosition);
                parsedMeasures[i] = measure;
                readPosition = checked(readPosition + measure.DataSize);
            }
            catch (Exception exception) when (exception is ArgumentException or OverflowException)
            {
                throw new ArgumentException($"Invalid Fumen V2 measure {i} at offset 0x{readPosition:X}.",
                    nameof(fumenData), exception);
            }
        }
    }

    public uint MeasureCount => ReadUInt32(fumenData, 0x200);
    public bool HasDivergentPaths => ReadUInt32(fumenData, 0x1B0) != 0;
    public uint MaxHp => ReadUInt32(fumenData, 0x1B4);
    public uint ClearHp => ReadUInt32(fumenData, 0x1B8);
    public int HpPerGood => ReadInt32(fumenData, 0x1BC);
    public int HpPerOk => ReadInt32(fumenData, 0x1C0);
    public int HpPerBad => ReadInt32(fumenData, 0x1C4);
    public uint MaxCombo => ReadUInt32(fumenData, 0x1C8);
    public uint MaxScoreValue => ReadUInt32(fumenData, 0x1FC);
    public IReadOnlyList<FumenMeasure> Measures => parsedMeasures;

    // 兼容现有调用方。
    public uint measureNum => MeasureCount;
    public bool hasDivision => HasDivergentPaths;
    public FumenMeasure[] measures => parsedMeasures;

    public void ResetJudgeTiming(EnsoData.EnsoLevelType level)
    {
        switch (level)
        {
            case EnsoData.EnsoLevelType.Easy:
            case EnsoData.EnsoLevelType.Normal:
                ResetJudgeTiming(41.7083358764648f, 108.441665649414f, 125.125f);
                break;
            case EnsoData.EnsoLevelType.Hard:
            case EnsoData.EnsoLevelType.Mania:
            case EnsoData.EnsoLevelType.Ura:
                ResetJudgeTiming(25.0250015258789f, 75.075004577637f, 108.441665649414f);
                break;
            case EnsoData.EnsoLevelType.Num:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public void ResetJudgeTiming(float good, float ok, float bad)
    {
        for (var i = 0; i < JudgeTimingCount; i++)
        {
            var offset = i * JudgeTimingSize;
            WriteSingle(fumenData, offset, good);
            WriteSingle(fumenData, offset + 4, ok);
            WriteSingle(fumenData, offset + 8, bad);
        }
    }

    public void MakeScrollSpeedEqual()
    {
        if (parsedMeasures.Length == 0) return;

        var baseBpm = parsedMeasures[0].Bpm;
        foreach (var measure in parsedMeasures)
        {
            var scrollSpeed = baseBpm / measure.Bpm;
            measure.SetAllBranchScrollSpeeds(scrollSpeed);
        }
    }

    public void MakeScrollSpeedRandom()
    {
        foreach (var measure in parsedMeasures)
            measure.SetAllBranchScrollSpeeds(Random.Shared.NextSingle() * 1.8f + 0.2f);
    }

    public void MakeScrollSpeedReverse()
    {
        foreach (var measure in parsedMeasures)
        {
            measure.NormalBranch.ScrollSpeed = -measure.NormalBranch.ScrollSpeed;
            measure.AdvancedBranch.ScrollSpeed = -measure.AdvancedBranch.ScrollSpeed;
            measure.MasterBranch.ScrollSpeed = -measure.MasterBranch.ScrollSpeed;
        }
    }

    public void MakeScrollSpeedSuperSlow()
    {
        const float divisor = 5f;
        foreach (var measure in parsedMeasures)
        {
            measure.NormalBranch.ScrollSpeed /= divisor;
            measure.AdvancedBranch.ScrollSpeed /= divisor;
            measure.MasterBranch.ScrollSpeed /= divisor;
        }
    }

    public MaxScore CalculateMaxScore()
    {
        var simpleNoteCount = 0;
        var bigNoteCount = 0;

        foreach (var measure in parsedMeasures)
        {
            var branch = HasDivergentPaths ? measure.MasterBranch : measure.NormalBranch;
            foreach (var note in branch.Notes)
            {
                if (!note.IsSimpleNote) continue;
                simpleNoteCount++;
                if (note.NoteType is Note.Type.BigDon or Note.Type.BigKatsu) bigNoteCount++;
            }
        }

        if (simpleNoteCount == 0) return default;

        // 保持原算法：取满足 score * noteCount > 100000 的最小整数，再以 10 为单位计分。
        var scoreUnits = 100_000 / simpleNoteCount + 1;
        var noteScore = checked(scoreUnits * 10);

        return new MaxScore
        {
            maxScore = checked(noteScore * simpleNoteCount),
            noteScore = noteScore,
            bigNoteAmount = bigNoteCount,
            simpleNoteAmount = simpleNoteCount
        };
    }

    public int GetTotalNotes()
    {
        var count = 0;
        foreach (var measure in parsedMeasures)
        {
            var branch = HasDivergentPaths ? measure.MasterBranch : measure.NormalBranch;
            foreach (var note in branch.Notes)
                if (note.IsSimpleNote)
                    count++;
        }

        return count;
    }

    public struct MaxScore
    {
        public int noteScore;
        public int simpleNoteAmount;
        public int bigNoteAmount;
        public int maxScore;
    }

    public sealed class FumenMeasure
    {
        private const int MeasureDataSize = 40;
        private readonly byte[] data;
        private readonly int index;

        internal FumenMeasure(byte[] data, int index)
        {
            EnsureRange(data, index, MeasureDataSize, "measure data");
            this.data = data;
            this.index = index;

            NormalBranch = new NoteData(data, index + MeasureDataSize);
            AdvancedBranch = new NoteData(data, checked(NormalBranch.Index + NormalBranch.DataSize));
            MasterBranch = new NoteData(data, checked(AdvancedBranch.Index + AdvancedBranch.DataSize));
            DataSize = checked(MeasureDataSize + NormalBranch.DataSize + AdvancedBranch.DataSize +
                               MasterBranch.DataSize);
        }

        public float Bpm
        {
            get => ReadSingle(data, index);
            set => WriteSingle(data, index, value);
        }

        public float Offset
        {
            get => ReadSingle(data, index + 4);
            set => WriteSingle(data, index + 4, value);
        }

        public bool IsGogoTime
        {
            get => data[index + 8] != 0;
            set => data[index + 8] = value ? (byte)1 : (byte)0;
        }

        public bool IsBarLineVisible
        {
            get => data[index + 9] != 0;
            set => data[index + 9] = value ? (byte)1 : (byte)0;
        }

        public uint NormalToAdvancedDivergePointRequirement => ReadUInt32(data, index + 0x0C);
        public uint NormalToMasterDivergePointRequirement => ReadUInt32(data, index + 0x10);
        public uint AdvancedToMasterDivergePointRequirement => ReadUInt32(data, index + 0x14);
        public uint AdvancedKeepAdvancedDivergePointRequirement => ReadUInt32(data, index + 0x18);
        public uint MasterToAdvancedDivergePointRequirement => ReadUInt32(data, index + 0x1C);
        public uint MasterKeepMasterDivergePointRequirement => ReadUInt32(data, index + 0x20);

        public NoteData NormalBranch { get; }
        public NoteData AdvancedBranch { get; }
        public NoteData MasterBranch { get; }
        public int DataSize { get; }

        // 兼容现有调用方。
        public float bpm { get => Bpm; set => Bpm = value; }
        public float offset { get => Offset; set => Offset = value; }
        public bool isGoGoTime { get => IsGogoTime; set => IsGogoTime = value; }
        public bool isBarLineVisible { get => IsBarLineVisible; set => IsBarLineVisible = value; }
        public NoteData normalNoteData => NormalBranch;
        public NoteData advanceNoteData => AdvancedBranch;
        public NoteData hardNoteData => MasterBranch;
        public int dataSize => DataSize;

        internal void SetAllBranchScrollSpeeds(float value)
        {
            NormalBranch.ScrollSpeed = value;
            AdvancedBranch.ScrollSpeed = value;
            MasterBranch.ScrollSpeed = value;
        }
    }

    public sealed class NoteData
    {
        private const int HeaderSize = 8;
        private readonly byte[] data;
        private readonly Note[] parsedNotes;

        internal NoteData(byte[] data, int index)
        {
            EnsureRange(data, index, HeaderSize, "measure branch data");
            this.data = data;
            Index = index;

            var count = NoteCount;
            parsedNotes = new Note[count];
            var readPosition = checked(index + HeaderSize);
            for (var i = 0; i < count; i++)
            {
                var note = new Note(data, readPosition);
                parsedNotes[i] = note;
                readPosition = checked(readPosition + note.DataSize);
            }

            DataSize = checked(readPosition - index);
        }

        internal int Index { get; }

        public ushort NoteCount => ReadUInt16(data, Index);

        public float ScrollSpeed
        {
            get => ReadSingle(data, Index + 4);
            set => WriteSingle(data, Index + 4, value);
        }

        public IReadOnlyList<Note> Notes => parsedNotes;
        public int DataSize { get; }

        // 兼容现有调用方。修改 NoteCount 会改变变长结构布局，因此不再提供不安全的 setter。
        public ushort noteNum => NoteCount;
        public float scrollSpeed { get => ScrollSpeed; set => ScrollSpeed = value; }
        public Note[] notes => parsedNotes;
        public int dataSize => DataSize;
    }

    public sealed class Note
    {
        private const int BaseSize = 24;
        private const int RendaSize = 32;
        private readonly byte[] data;
        private readonly int index;

        internal Note(byte[] data, int index)
        {
            EnsureRange(data, index, BaseSize, "note data");
            this.data = data;
            this.index = index;
            DataSize = GetDataSize(NoteType);
            EnsureRange(data, index, DataSize, "note data");
        }

        public enum Type : uint
        {
            Don = 1,
            Do = 2,
            Ko = 3,
            Katsu = 4,
            Ka = 5,
            Renda = 6,
            BigDon = 7,
            BigKatsu = 8,
            BigRenda = 9,
            Balloon = 10,
            Bell = 12
        }

        public Type NoteType
        {
            get => (Type)ReadUInt32(data, index);
            set
            {
                if (GetDataSize(value) != DataSize)
                    throw new InvalidOperationException("Changing between fixed-size and renda notes requires resizing the chart.");
                WriteUInt32(data, index, (uint)value);
            }
        }

        public float NoteOffset
        {
            get => ReadSingle(data, index + 4);
            set => WriteSingle(data, index + 4, value);
        }

        // fumenv2.hexpat: InitialScoreValue 位于 +0x0C，+0x10 是 padding。
        public ushort InitialScoreValue
        {
            get => ReadUInt16(data, index + 0x0C);
            set => WriteUInt16(data, index + 0x0C, value);
        }

        public ushort ScoreDifferenceTimes4
        {
            get => ReadUInt16(data, index + 0x0E);
            set => WriteUInt16(data, index + 0x0E, value);
        }

        public int ScoreDifference
        {
            get => ScoreDifferenceTimes4 / 4;
            set => ScoreDifferenceTimes4 = checked((ushort)(value * 4));
        }

        public float Length
        {
            get => ReadSingle(data, index + 0x14);
            set => WriteSingle(data, index + 0x14, value);
        }

        public int DataSize { get; }

        public bool IsSimpleNote => NoteType is Type.Don or Type.Do or Type.Ko or Type.Katsu or Type.Ka or
            Type.BigDon or Type.BigKatsu;

        // 气球/铃铛次数与连打相关值复用格式中的 InitialScoreValue，并不存在 +0x18 的额外字段。
        public int RendaHitCount => InitialScoreValue;
        public ushort BalloonCount => InitialScoreValue;

        // 兼容现有调用方。
        public Type noteType { get => NoteType; set => NoteType = value; }
        public float noteOffset { get => NoteOffset; set => NoteOffset = value; }
        public int randaHitsCount => RendaHitCount;
        public int initialScoreValue { get => InitialScoreValue; set => InitialScoreValue = checked((ushort)value); }
        public int scoreDifference { get => ScoreDifference; set => ScoreDifference = value; }
        public float rendaLength { get => Length; set => Length = value; }
        public ushort balloonCount { get => BalloonCount; set => InitialScoreValue = value; }
        public int dataSize => DataSize;
        public bool isSimpleNote => IsSimpleNote;

        private static int GetDataSize(Type type) => type is Type.Renda or Type.BigRenda ? RendaSize : BaseSize;
    }

    private static void EnsureRange(byte[] data, int offset, int length, string structureName)
    {
        if (offset < 0 || length < 0 || offset > data.Length - length)
            throw new ArgumentException(
                $"The {structureName} at 0x{offset:X} extends beyond the {data.Length}-byte buffer.");
    }

    private static ushort ReadUInt16(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)));

    private static uint ReadUInt32(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, sizeof(uint)));

    private static int ReadInt32(byte[] data, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, sizeof(int)));

    private static float ReadSingle(byte[] data, int offset) =>
        BitConverter.Int32BitsToSingle(ReadInt32(data, offset));

    private static void WriteUInt16(byte[] data, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)), value);

    private static void WriteUInt32(byte[] data, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, sizeof(uint)), value);

    private static void WriteSingle(byte[] data, int offset, float value) =>
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset, sizeof(float)), BitConverter.SingleToInt32Bits(value));
}

internal static class FumenDataPlayerData
{
    private static readonly IntPtr NativeFieldInfoPtr_fumenData;

    static FumenDataPlayerData()
    {
        NativeFieldInfoPtr_fumenData =
            IL2CPP.GetIl2CppField(Il2CppClassPointerStore<FumenLoader.PlayerData>.NativeClassPtr, "fumenData");
    }

    // Il2CppInterop 生成的获取谱面数据指针的方法有误，这里手动实现一个。
    public static byte[] GetFumenDataAsBytes(this FumenLoader.PlayerData playerData)
    {
        if (!playerData.isReadSucceed) throw new FumenNoLoadedException();
        unsafe
        {
            var ptr = IL2CPP.Il2CppObjectBaseToPtrNotNull(playerData) +
                      (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_fumenData);
            var ptrBytes = *(IntPtr*)ptr;
            var length = playerData.fumenSize;
            var bytes = new byte[length];
            Marshal.Copy(ptrBytes, bytes, 0, length);
            return bytes;
        }
    }
}

internal class FumenNoLoadedException : Exception
{
    public FumenNoLoadedException() : base("Fumen data is not read successfully.")
    {
    }
}

public static class FumenLoaderExt
{
    public static FumenReader GetFumenReader(this FumenLoader fumenLoader, int player = 0)
    {
        unsafe
        {
            var dataSize = fumenLoader.GetFumenSize(player);
            var dataPtr = fumenLoader.GetFumenData(player);
            if (dataPtr == null || dataSize <= 0)
                throw new NullReferenceException();
            var data = new byte[dataSize];
            Marshal.Copy((IntPtr)dataPtr, data, 0, dataSize);
            return new FumenReader(data);
        }
    }
}
