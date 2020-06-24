using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Models
{
    // TODO: Delete, not in use.
    internal class VolumeItemModel : IEquatable<VolumeItemModel>
    {
        #region Constructor
        public VolumeItemModel(int pid, string displayName, int volumeLevel, bool isMuted)
        {
            PID = pid;
            DisplayName = displayName;
            VolumeLevel = volumeLevel;
            IsMuted = isMuted;
        }
        #endregion


        #region Fields
        public readonly int PID;
        public readonly string DisplayName;
        public int VolumeLevel;
        public bool IsMuted;

        public bool Equals(VolumeItemModel other)
        {
            return other.PID == PID;
        }
        #endregion
    }
}
