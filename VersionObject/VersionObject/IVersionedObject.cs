namespace VersionObject
{
    /// <summary>
    /// Interface to control and nagivate through versions of the same object
    /// </summary>
    /// <typeparam name="T">Interface of properties set to version</typeparam>
    public interface IVersionedObject<out T> where T:class
    {
        int LastVersionId { get; }

        bool HasPendingChanges { get; }

        /// <summary>
        /// Gets/Sets object version id in order to browse its different states
        /// </summary>
        int CurrentVersionId { get; set; }

        /// <summary>
        /// Gets a proxy on the original object
        /// </summary>
        T CurrentState { get;}

        /// <summary>
        /// Records all of the pending changes in a new version
        /// </summary>
        void CommitToNewVersion();

        /// <summary>
        /// Suppresses all of the pending changes
        /// </summary>
        void RollBackChanges();

        /// <summary>
        /// Commits all of recorded changes to the original object up to  <param name="versionId"/>
        /// </summary>
        /// <param name="versionId"></param>
        void Cristallize(int versionId);
    }
}
