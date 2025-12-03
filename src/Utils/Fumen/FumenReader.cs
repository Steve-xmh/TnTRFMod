using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;

namespace TnTRFMod.Utils.Fumen;

public record FumenReader(byte[] fumenData)
{
    public uint measureNum => BitConverter.ToUInt32(fumenData, 0x200);

    public FumenMeasure[] measures
    {
        get
        {
            var measureNum = this.measureNum;
            var measures = new FumenMeasure[measureNum];

            var readPos = 0x208;
            for (var i = 0; i < measureNum; i++)
            {
                measures[i] = new FumenMeasure(fumenData, readPos);
                readPos += measures[i].dataSize;
            }

            return measures;
        }
    }

    public bool hasDivision => BitConverter.ToInt32(fumenData, 0x1B0) == 1;

    public void ResetJudgeTiming(EnsoData.EnsoLevelType level)
    {
        switch (level)
        {
            case EnsoData.EnsoLevelType.Easy:
            case EnsoData.EnsoLevelType.Normal:
                ResetJudgeTiming(41.7083358764648f, 108.441665649414f, 125.125000000000f);
                break;
            case EnsoData.EnsoLevelType.Hard:
            case EnsoData.EnsoLevelType.Mania:
            case EnsoData.EnsoLevelType.Ura:
                ResetJudgeTiming(25.0250015258789f, 075.075004577637f, 108.441665649414f);
                break;
            case EnsoData.EnsoLevelType.Num:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public void ResetJudgeTiming(float good, float ok, float bad)
    {
        var goodBytes = BitConverter.GetBytes(good);
        var okBytes = BitConverter.GetBytes(ok);
        var badBytes = BitConverter.GetBytes(bad);
        for (var i = 0; i < 36; i++)
        {
            goodBytes.CopyTo(fumenData, i * 4 * 3);
            okBytes.CopyTo(fumenData, i * 4 * 3 + 4);
            badBytes.CopyTo(fumenData, i * 4 * 3 + 8);
        }
    }

    public void MakeScrollSpeedEqual()
    {
        var bpm = measures[0].bpm;
        foreach (var readerMeasure in measures)
        {
            var bpmAspect = bpm / readerMeasure.bpm;
            readerMeasure.normalNoteData.scrollSpeed = bpmAspect;
            readerMeasure.hardNoteData.scrollSpeed = bpmAspect;
            readerMeasure.advanceNoteData.scrollSpeed = bpmAspect;
        }
    }

    public void MakeScrollSpeedRandom()
    {
        foreach (var readerMeasure in measures)
        {
            var multiplier = Random.Shared.NextSingle() * 1.8f + 0.2f;
            readerMeasure.normalNoteData.scrollSpeed = multiplier;
            readerMeasure.hardNoteData.scrollSpeed = multiplier;
            readerMeasure.advanceNoteData.scrollSpeed = multiplier;
        }
    }

    public void MakeScrollSpeedReverse()
    {
        foreach (var readerMeasure in measures)
        {
            readerMeasure.normalNoteData.scrollSpeed = -readerMeasure.normalNoteData.scrollSpeed;
            readerMeasure.hardNoteData.scrollSpeed = -readerMeasure.hardNoteData.scrollSpeed;
            readerMeasure.advanceNoteData.scrollSpeed = -readerMeasure.advanceNoteData.scrollSpeed;
        }
    }

    public void MakeScrollSpeedSuperSlow()
    {
        var maxMultiplier = 5f;
        // var shouldReduce = true;
        // const float minScrollSpeed = float.Epsilon;
        // while (shouldReduce)
        // {
        //     shouldReduce = measures.Any(readerMeasure =>
        //         readerMeasure.normalNoteData.scrollSpeed / maxMultiplier < minScrollSpeed ||
        //         readerMeasure.hardNoteData.scrollSpeed / maxMultiplier < minScrollSpeed ||
        //         readerMeasure.advanceNoteData.scrollSpeed / maxMultiplier < minScrollSpeed);
        //     if (shouldReduce) maxMultiplier -= 1f;
        // }
        //
        // Logger.Info("maxMultiplier: " + maxMultiplier);

        foreach (var readerMeasure in measures)
        {
            readerMeasure.normalNoteData.scrollSpeed /= maxMultiplier;
            readerMeasure.hardNoteData.scrollSpeed /= maxMultiplier;
            readerMeasure.advanceNoteData.scrollSpeed /= maxMultiplier;
        }
    }

    public MaxScore CalculateMaxScore()
    {
        var balloonAmount = 0;
        var balloonHitAmount = 0;
        var simpleNoteAmount = 0;
        var bigNoteAmount = 0;
        var rendaTotalSmallLength = 0f;
        var rendaTotalBigLength = 0f;
        var rendaTotalSmallHitCount = 0;
        var rendaTotalBigHitCount = 0;
        var rendaNoteAmount = 0;
        var simpleInitScore = 0;

        foreach (var measure in measures)
        {
            var notes = hasDivision ? measure.hardNoteData : measure.normalNoteData;
            foreach (var note in notes.notes)
                switch (note.noteType)
                {
                    case Note.Type.Don:
                    case Note.Type.Do:
                    case Note.Type.Ko:
                    case Note.Type.Katsu:
                    case Note.Type.Ka:
                        simpleNoteAmount++;
                        simpleInitScore += note.initialScoreValue;
                        break;
                    case Note.Type.BigDon:
                    case Note.Type.BigKatsu:
                        simpleNoteAmount++;
                        bigNoteAmount++;
                        simpleInitScore += note.initialScoreValue * 2;
                        // Logger.Info($"SimpleNoteScore {note.initialScoreValue} {note.scoreDifference}");
                        break;
                    case Note.Type.Renda:
                        rendaNoteAmount++;
                        rendaTotalSmallLength += (int)note.rendaLength;
                        rendaTotalSmallHitCount += note.randaHitsCount;
                        break;
                    case Note.Type.BigRenda:
                        rendaNoteAmount++;
                        rendaTotalBigLength += (int)note.rendaLength;
                        rendaTotalBigHitCount += note.randaHitsCount;
                        break;
                    case Note.Type.Balloon:
                    case Note.Type.Bell:
                        balloonAmount++;
                        rendaNoteAmount++;
                        balloonHitAmount += note.balloonCount;
                        rendaTotalSmallHitCount += note.randaHitsCount;
                        rendaTotalSmallLength += (int)note.rendaLength;
                        break;
                }
        }

        var noteScore = 0;

        if (simpleNoteAmount == 0)
            return new MaxScore
            {
                maxScore = 0,
                noteScore = 0,
                bigNoteAmount = 0,
                simpleNoteAmount = 0
            };

        while (noteScore * simpleNoteAmount <= 100_000) noteScore += 1;

        var result = 10 * noteScore * simpleNoteAmount;

        // Console.Out.WriteLine(
        //     $"simpleNoteAmount: {simpleNoteAmount} " +
        //     $"rendaTotalSmallLength: {rendaTotalSmallLength} " +
        //     $"rendaTotalBigLength: {rendaTotalBigLength} " +
        //     $"rendaTotalSmallHitCount: {rendaTotalSmallHitCount} " +
        //     $"rendaTotalBigHitCount: {rendaTotalBigHitCount} " +
        //     $"noteScore: {noteScore} " +
        //     $"simpleInitScore: {simpleInitScore} " +
        //     $"balloonHitAmount: {balloonHitAmount} ");

        // Logger.Message(
        //     $"FumenReader.CalculateMaxScore: {result}");

        // if (result < 100_0000)
        //     Logger.Warn($"Warning: Fumen score is less than 1000000: {result}");

        return new MaxScore
        {
            maxScore = result,
            noteScore = noteScore * 10,
            bigNoteAmount = bigNoteAmount,
            simpleNoteAmount = simpleNoteAmount
        };
    }

    public static void FumenTest()
    {
        string[] testList =
        [
            "C:/Program Files (x86)/Steam/steamapps/common/Taiko no Tatsujin Rhythm Festival/TnTRFMod/twcfsp_m.bin",
            "C:/Program Files (x86)/Steam/steamapps/common/Taiko no Tatsujin Rhythm Festival/TnTRFMod/gunsln_m.bin",
            "C:/Program Files (x86)/Steam/steamapps/common/Taiko no Tatsujin Rhythm Festival/TnTRFMod/crkvic_m.bin",
            "C:/Program Files (x86)/Steam/steamapps/common/Taiko no Tatsujin Rhythm Festival/TnTRFMod/tdm_x.bin"
        ];

        foreach (var testFile in testList)
        {
            if (!File.Exists(testFile))
            {
                Console.Out.WriteLine($"Test file not found: {testFile}");
                continue;
            }

            var fumenData = File.ReadAllBytes(testFile);
            var reader = new FumenReader(fumenData);
            Console.Out.WriteLine($"Fumen Test: {testFile} - Measure Count: {reader.measureNum}");
            Console.Out.WriteLine($"Max Score: {reader.CalculateMaxScore()}");
        }
    }

    public int GetTotalNotes()
    {
        return measures.Select(measure => hasDivision ? measure.hardNoteData : measure.normalNoteData)
            .Select(notes => notes.notes.Count(n => n.isSimpleNote)).Sum();
    }

    public struct MaxScore
    {
        public int noteScore;
        public int simpleNoteAmount;
        public int bigNoteAmount;
        public int maxScore;
    }

    public record FumenMeasure(byte[] fumenData, int measureDataIndex)
    {
        public float bpm
        {
            get => BitConverter.ToSingle(fumenData, measureDataIndex);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, measureDataIndex);
        }

        public float offset
        {
            get => BitConverter.ToSingle(fumenData, measureDataIndex + 4);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, measureDataIndex + 4);
        }

        public bool isGoGoTime
        {
            get => BitConverter.ToBoolean(fumenData, measureDataIndex + 8);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, measureDataIndex + 8);
        }

        public bool isBarLineVisible
        {
            get => BitConverter.ToBoolean(fumenData, measureDataIndex + 9);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, measureDataIndex + 9);
        }

        public NoteData normalNoteData => new(fumenData, measureDataIndex + 40);
        public NoteData advanceNoteData => new(fumenData, measureDataIndex + 40 + normalNoteData.dataSize);

        public NoteData hardNoteData => new(fumenData,
            measureDataIndex + 40 + normalNoteData.dataSize + advanceNoteData.dataSize);

        public int dataSize => 40 + normalNoteData.dataSize + advanceNoteData.dataSize + hardNoteData.dataSize;
    }

    public record NoteData(byte[] fumenData, int noteDataIndex)
    {
        public ushort noteNum
        {
            get => BitConverter.ToUInt16(fumenData, noteDataIndex);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, noteDataIndex);
        }

        public float scrollSpeed
        {
            get => BitConverter.ToSingle(fumenData, noteDataIndex + 4);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, noteDataIndex + 4);
        }

        public Note[] notes
        {
            get
            {
                var noteNum = this.noteNum;
                var notes = new Note[noteNum];

                var readPos = noteDataIndex + 8;
                for (var i = 0; i < noteNum; i++)
                {
                    notes[i] = new Note(fumenData, readPos);
                    readPos += notes[i].dataSize;
                }

                return notes;
            }
        }

        public int dataSize => 8 + notes.Sum(n => n.dataSize);
    }

    public record Note(byte[] fumenData, int noteIndex)
    {
        public enum Type
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

        public Type noteType
        {
            get => (Type)BitConverter.ToInt32(fumenData, noteIndex);
            set => BitConverter.GetBytes((int)value).CopyTo(fumenData, noteIndex);
        }

        public float noteOffset
        {
            get => BitConverter.ToSingle(fumenData, noteIndex + 4);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, noteIndex + 4);
        }

        public int randaHitsCount => initialScoreValue;

        public int initialScoreValue
        {
            get => BitConverter.ToUInt16(fumenData, noteIndex + 0x10);
            set => BitConverter.GetBytes((ushort)value).CopyTo(fumenData, noteIndex + 0x10);
        }

        public int scoreDifference
        {
            get => BitConverter.ToUInt16(fumenData, noteIndex + 0x12) / 4;
            set => BitConverter.GetBytes((ushort)(value * 4)).CopyTo(fumenData, noteIndex + 0x12);
        }

        public float rendaLength
        {
            get => BitConverter.ToSingle(fumenData, noteIndex + 20);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, noteIndex + 20);
        }

        public ushort balloonCount
        {
            get => BitConverter.ToUInt16(fumenData, noteIndex + 24);
            set => BitConverter.GetBytes(value).CopyTo(fumenData, noteIndex + 24);
        }

        public int dataSize => noteType is Type.Renda or Type.BigRenda ? 32 : 24;

        public bool isSimpleNote =>
            noteType is Type.Don or Type.Do or Type.Ko or Type.Katsu or Type.Ka or Type.BigDon or Type.BigKatsu;
    }
}

internal static class FumenDataPlayerData
{
    private static readonly IntPtr NativeFieldInfoPtr_fumenData;

    static FumenDataPlayerData()
    {
        NativeFieldInfoPtr_fumenData =
            IL2CPP.GetIl2CppField(Il2CppClassPointerStore<FumenLoader.PlayerData>.NativeClassPtr, "fumenData");
    }

    // Il2cppInterop 生成的获取铺面数据指针的方法有误，这里手动实现一个
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