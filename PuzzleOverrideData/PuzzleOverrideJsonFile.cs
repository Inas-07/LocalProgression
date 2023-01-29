
using System.Collections.Generic;

namespace ScanPosOverride.PuzzleOverrideData
{
    internal sealed class PuzzleOverrideJsonFile
    {
        public uint MainLevelLayout { get; set; }

        public List<PuzzleOverride> Puzzles { get; set; } = new List<PuzzleOverride>();
    }
}
