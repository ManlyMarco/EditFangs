using MessagePack;

namespace EditFangs
{
    [MessagePackObject]
    public class FangData
    {
        [IgnoreMember]
        internal bool dirty;

        [Key(0)]
        public float scaleL
        {
            get => _scaleL; set { _scaleL = value; dirty = true; }
        }

        [Key(1)]
        public float scaleR
        {
            get => _scaleR; set { _scaleR = value; dirty = true; }
        }

        [Key(2)]
        public float spacingL
        {
            get => _spacingL; set { _spacingL = value; dirty = true; }
        }

        [Key(3)]
        public float spacingR
        {
            get => _spacingR; set { _spacingR = value; dirty = true; }
        }

        private static readonly FangData Empty = new FangData();
        private float _scaleL = 0.1f;
        private float _scaleR = 0.1f;
        private float _spacingL = 1f;
        private float _spacingR = 1f;

        public bool IsEmpty()
        {
            return Empty.Equals(this);
        }
        public bool Equals(FangData other)
        {
            return scaleL.Equals(other.scaleL) && scaleR.Equals(other.scaleR) && spacingL.Equals(other.spacingL) && spacingR.Equals(other.spacingR);
        }
    }
}
