using Microsoft.VisualStudio.TestTools.UnitTesting;
using VersionObject;

namespace VersionObjectTests
{
    [TestClass]
    public class VersionnedObjectTest
    {
        public interface IToto
        {
            int Id { get; set; }

            string Value { get; set; }
        }

        private class Toto : IToto
        {
            private int _id;
            private string _value;

            public int Id
            {
                get
                {
                    return _id;
                }
                set
                {
                    _id = value;
                }
            }

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                }
            }
        }

        [TestMethod]
        public void TestVersionning()
        {
            var t = ObjectVersionner.GetVersionedObject<IToto>(new Toto() {Id = 666, Value = "original"});

            t.CurrentState.Id = 1;
            t.CurrentState.Value = "sample";

            int a = t.CurrentState.Id;
            string b = t.CurrentState.Value;

            Assert.AreEqual("sample",b);
            Assert.AreEqual(a, 1);

            Assert.IsTrue(t.HasPendingChanges);
            t.CommitToNewVersion();
            Assert.IsFalse(t.HasPendingChanges);

            Assert.IsTrue(t.LastVersionId == 1);

            //move backward to version 0
            t.CurrentVersionId = 0;

            int c = t.CurrentState.Id;
            string d = t.CurrentState.Value;

            Assert.AreEqual("original", d);
            Assert.AreEqual(c, 666);

            //move forward to latest version
            t.MoveToLatestVersion();

            int e = t.CurrentState.Id;
            string f = t.CurrentState.Value;

            Assert.AreEqual("sample", f);
            Assert.AreEqual(e, 1);


        }

        [TestMethod]
        public void TestRollBack()
        {
            var t = ObjectVersionner.GetVersionedObject<IToto>(new Toto() { Id = 666, Value = "original" });

            t.CurrentState.Id = 1;
            t.CurrentState.Value = "sample";

            int a = t.CurrentState.Id;
            string b = t.CurrentState.Value;

            Assert.AreEqual("sample", b);
            Assert.AreEqual(a, 1);

            Assert.IsTrue(t.HasPendingChanges);
            t.RollBackChanges();
            Assert.IsFalse(t.HasPendingChanges);

            Assert.AreEqual("original", t.CurrentState.Value);
            Assert.AreEqual(666, t.CurrentState.Id);
        }
    }
}
