using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Match3Sample.Gameplay.Player.Stats
{
    [System.Serializable]
    public class PlayerStats
    {
        public string PlayerID { get; set; }
        public string PlayerName { get; set; }
        public CharacterType CharacterType { get; set; }
        public CharacterState[] CharacterStates { get; set; }
        public long Gold { get; set; }
        public long Money { get; set; }
        public int DailySpin { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int AvailableTurns { get; set; }
        public int BestAttackScore { get; set; }
        public int BestDefenceScore { get; set; }
        public int ELOPoints { get; set; }
        public int WorldRank { get; set; }
        public int HealthPoints { get; set; }
        public int AttackPoints { get; set; }
        public int DefencePoints { get; set; }
        public int ShuffleCount { get; set; }
        public int HintsCount { get; set; }
        public int AttackCount { get; set; }
        public int DefenceCount { get; set; }
        public int SpecialtyCount { get; set; }
        private static XmlSerializer serializer = new XmlSerializer(typeof(PlayerStats));

        public PlayerStats()
        {
            PlayerID = null;
            PlayerName = "Player";
            CharacterType = CharacterType.LeiGong;
            CharacterStates = new CharacterState[8];
            for (int i = 0; i < CharacterStates.Length; i++)
                CharacterStates[i] = CharacterState.Unlocked;
            CharacterStates[3] = CharacterState.Locked;
            Gold = 0;
            Money = 0;
            Wins = 0;
            Losses = 0;
            Draws = 0;
            AvailableTurns = 5;
            DailySpin = 0;
            BestAttackScore = 0;
            BestDefenceScore = 0;
            ELOPoints = 0;
            WorldRank = 1;
            HealthPoints = 200;
            AttackPoints = 0;
            DefencePoints = 0;
            ShuffleCount = 3;
            HintsCount = 3;
            AttackCount = 1;
            DefenceCount = 1;
            SpecialtyCount = 1;

        }

        public void Reset()
        {
            HealthPoints = 200;
            AttackPoints = 0;
            DefencePoints = 0;
        }

        public string Serialize()
        {
            StringBuilder builder = new StringBuilder();
            serializer.Serialize(XmlWriter.Create(builder), this);
            return builder.ToString();
        }

        public static PlayerStats Deserialize(string serializedData)
        {
            return serializer.Deserialize(new StringReader(serializedData)) as PlayerStats;
        }
    }

    public enum CharacterType
    {
        Chaac,
        Indra,
        LeiGong,
        Odin,
        Perun,
        Thor,
        Zeus,
        Raijin
    }

    public enum CharacterState
    {
        Locked,
        Unlocked
    }
}