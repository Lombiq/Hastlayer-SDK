namespace Hast.Samples.SampleAssembly.Lzma
{
    public struct CoderState
    {
        public uint Index { get; set; }


        public void Init() =>
            Index = 0;

        public void UpdateChar()
        {
            if (Index < 4) Index = 0;
            else if (Index < 10) Index -= 3;
            else Index -= 6;
        }

        public void UpdateMatch() =>
            Index = (uint)(Index < 7 ? 7 : 10);

        public void UpdateRep() =>
            Index = (uint)(Index < 7 ? 8 : 11);

        public void UpdateShortRep() =>
            Index = (uint)(Index < 7 ? 9 : 11);

        public bool IsCharState() =>
            Index < 7;
    }
}
