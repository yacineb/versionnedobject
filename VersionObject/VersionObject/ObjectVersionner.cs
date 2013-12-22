using System.Runtime.InteropServices.ComTypes;

namespace VersionObject
{
    public static class ObjectVersionner
    {
        public static IVersionedObject<T> GetVersionedObject<T>(T obj) where T : class
        {
            return new VersionedObject<T>(obj);
        }

        #region Extension Methods

        public static void MoveToLatestVersion<T>(this IVersionedObject<T> @this) where T : class
        {
            @this.CurrentVersionId = @this.LastVersionId;
        }
        public static void MoveToFirstVersion<T>(this IVersionedObject<T> @this) where T : class
        {
            @this.CurrentVersionId = 0;
        }
        #endregion

    }
}