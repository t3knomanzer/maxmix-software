namespace MaxMix.Services.Audio
{
    public interface ISession
    {
        /// <summary>
        /// The computed Identifier for this session.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The display name of the process that created this session.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Is this the default windows device for this Flow
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// Current volume of this session (0-100).
        /// </summary>
        int Volume { get; set; }

        /// <summary>
        /// Current mute state of this session.
        /// </summary>
        bool IsMuted { get; set; }
    }
}
