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

    public int CalculateMaxScore()
    {
        var balloonAmount = 0;
        var balloonHitAmount = 0;
        var simpleNoteAmount = 0;
        var rendaTotalLength = 0f;

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
                    case Note.Type.BigDon:
                    case Note.Type.BigKatsu:
                        simpleNoteAmount++;
                        break;
                    case Note.Type.Renda:
                    case Note.Type.BigRenda:
                        rendaTotalLength += (int)note.rendaLength;
                        break;
                    case Note.Type.Balloon:
                    case Note.Type.Bell:
                        balloonAmount++;
                        balloonHitAmount += note.balloonCount;
                        break;
                }
        }

        var noteScore = 0;

        while (noteScore * simpleNoteAmount <= 100_000) noteScore += 1;

        if (balloonHitAmount + rendaTotalLength > 0f)
            noteScore -= 1;

        var result = 10 * (noteScore * simpleNoteAmount +
                           (balloonHitAmount + (int)(rendaTotalLength * 21f / 1000f)) * 10);

        if (result < 100_0000)
            Console.Out.WriteLine($"Warning: Fumen score is less than 1000000: {result}");
        
        return result;
    }

    public int GetTotalNotes()
    {
        return measures.Select(measure => hasDivision ? measure.hardNoteData : measure.normalNoteData)
            .Select(notes => notes.notes.Count(n => n.isSimpleNote)).Sum();
    }

    public record FumenMeasure(byte[] fumenData, int measureDataIndex)
    {
        public float bpm => BitConverter.ToSingle(fumenData, measureDataIndex);
        public float offset => BitConverter.ToSingle(fumenData, measureDataIndex + 4);
        public bool isGoGoTime => BitConverter.ToBoolean(fumenData, measureDataIndex + 8);
        public bool isBarLineVisible => BitConverter.ToBoolean(fumenData, measureDataIndex + 9);

        public NoteData normalNoteData => new(fumenData, measureDataIndex + 40);
        public NoteData advanceNoteData => new(fumenData, measureDataIndex + 40 + normalNoteData.dataSize);

        public NoteData hardNoteData => new(fumenData,
            measureDataIndex + 40 + normalNoteData.dataSize + advanceNoteData.dataSize);

        public int dataSize => 40 + normalNoteData.dataSize + advanceNoteData.dataSize + hardNoteData.dataSize;
    }

    public record NoteData(byte[] fumenData, int noteDataIndex)
    {
        public ushort noteNum => BitConverter.ToUInt16(fumenData, noteDataIndex);
        public float scrollSpeed => BitConverter.ToSingle(fumenData, noteDataIndex + 4);

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

        public Type noteType => (Type)BitConverter.ToInt32(fumenData, noteIndex);
        public float noteOffset => BitConverter.ToSingle(fumenData, noteIndex + 4);
        public int initialScoreValue => BitConverter.ToUInt16(fumenData, noteIndex + 12);
        public int scoreDifference => BitConverter.ToUInt16(fumenData, noteIndex + 16) / 4;
        public float rendaLength => BitConverter.ToSingle(fumenData, noteIndex + 20);
        public ushort balloonCount => BitConverter.ToUInt16(fumenData, noteIndex + 24);

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