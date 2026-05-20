namespace Content.Shared.Roles
{
    public sealed partial class JobPrototype
    {
        /// <summary>
        /// How much currency will be added when the person on this job finishes the round
        /// </summary>
        [DataField]
        public int Currency = 1; // everyone gets one.
    }
}
