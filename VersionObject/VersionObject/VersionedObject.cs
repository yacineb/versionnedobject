using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using FastMember;

namespace VersionObject
{
    internal class VersionedObject<T> : IVersionedObject<T> 
        where T : class
    {
        static readonly PropertyInfo[] Properties
            = typeof (T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        private readonly T _original;
        private readonly T _proxiedType;
        private readonly ObjectAccessor _originalObjectAccessor;

        private readonly Dictionary<string, Dictionary<int, List<Modification>>> _modifs;
        private readonly Dictionary<string, List<Modification>> _pendingModifs;

        private readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

        public VersionedObject(T original)
        {
            if (original == null)
                throw new ArgumentNullException("original");


            LastVersionId = 0;
            _original = original;
            _originalObjectAccessor = FastMember.ObjectAccessor.Create(_original);

            _modifs = new Dictionary<string, Dictionary<int, List<Modification>>>();
            _pendingModifs = new Dictionary<string, List<Modification>>();

            foreach (PropertyInfo prop in Properties)
            {
                object propvalue = _originalObjectAccessor[prop.Name];
                _modifs.Add(prop.Name, new Dictionary<int, List<Modification>>()
                {
                    { 0, new List<Modification> { new Modification(prop.Name, propvalue) } }
                });
                _pendingModifs.Add(prop.Name, new List<Modification>());
            }

            _proxiedType = _proxyGenerator.CreateInterfaceProxyWithTarget(_original, new ModificationInterceptor(this));
        }

        public int LastVersionId { get; private set; }
        public bool HasPendingChanges { get { return _pendingModifs.Any(kvp => kvp.Value.Count > 0); } }

        private int _currentVersionId;
        public int CurrentVersionId
        {
            get
            {
                return _currentVersionId;
            }
            set
            {
                VerifyVersion(value);
                _currentVersionId = value;
            }
        }

        public T CurrentState { get { return _proxiedType; } }

        public void CommitToNewVersion()
        {
            if (!HasPendingChanges) return;
            ++LastVersionId;

            //register permenently new modifications
            foreach (KeyValuePair<string, List<Modification>> kvp in _pendingModifs)
            {
                _modifs[kvp.Key][LastVersionId] = new List<Modification>(kvp.Value);
            }
            ClearPendingChanges();

        }

        public void RollBackChanges()
        {
            if (!HasPendingChanges) return;

            ClearPendingChanges();
        }

        public void Cristallize(int versionId)
        {
            VerifyVersion(versionId);

            foreach (KeyValuePair<string, Dictionary<int, List<Modification>>> kvp in _modifs)
            {
                string propName = kvp.Key;
                for (int i = 0; i < versionId+1; i++)//for each modification set in a version
                {
                    foreach (Modification modif in kvp.Value[i])
                    {
                        _originalObjectAccessor[propName] = modif.NewVal;
                    }
                    if (i < versionId) kvp.Value.Remove(i);
                }
            }

            LastVersionId = 0;
        }

        private void VerifyVersion(int versionId)
        {
            if (versionId < 0 || versionId > LastVersionId)
                throw new InvalidOperationException("Invalid version id:" + versionId);
        }


        private void ClearPendingChanges()
        {
            foreach (List<Modification> modif in _pendingModifs.Values)
            {
                modif.Clear();
            }
        }

        void CaptureModification(string propName, object newVal)
        {
            _pendingModifs[propName].Add(new Modification(propName, newVal));
        }

         Modification GetLastModification(string propertyName)
        {
            return _modifs[propertyName][CurrentVersionId].Concat(_pendingModifs[propertyName]).LastOrDefault();
        }
        #region Nested

        sealed class Modification
        {
            public string PropertyId { get; private set; }
            public object NewVal { get; private set; }
            public DateTime ModificationTime { get; private set; }

            public Modification(string propertyId, object newVal)
            {
                PropertyId = propertyId;
                NewVal = newVal;
                ModificationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// This class modifies behavior of the getter/setter methods
        /// </summary>
        class ModificationInterceptor : IInterceptor
        {
            private readonly VersionedObject<T> _verionedObj;

            public ModificationInterceptor(VersionedObject<T> verionedObj)
            {
                _verionedObj = verionedObj;
            }

            public void Intercept(IInvocation invocation)
            {
                if (IsGetter(invocation.Method))
                {
                    invocation.ReturnValue = _verionedObj.GetLastModification(invocation.Method.Name.Substring(4)).NewVal;
                }
                else if (IsSetter(invocation.Method))
                {
                    _verionedObj.CaptureModification(invocation.Method.Name.Substring(4), invocation.Arguments[0]);
                }
                else
                   invocation.Proceed();
                
            }

            //following ecma specification getter and setter methods generated by c# compiler
            //have the following prefixes : get_  and set_
            private bool IsGetter(MethodInfo method)
            {
                return method.IsSpecialName && method.Name.Substring(0, 4) == "get_";
            }
            private bool IsSetter(MethodInfo method)
            {
                return method.IsSpecialName && method.Name.Substring(0, 4) == "set_";
            }

        }
        #endregion

    }
}
