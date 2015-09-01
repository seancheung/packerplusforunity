using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ultralpha.Editor
{
    /// <summary>
    /// A wrapped undo record
    /// </summary>
    [Serializable]
    public class UndoObject : IDisposable
    {
        [SerializeField] private Object obj;
        private bool calledCheck;

        /// <summary>
        /// object to record
        /// </summary>
        /// <param name="obj"></param>
        public UndoObject(Object obj)
        {
            this.obj = obj;
        }

        /// <summary>
        /// Record object undo
        /// </summary>
        /// <param name="name">undo name</param>
        /// <returns></returns>
        public IDisposable Record(string name)
        {
            Undo.RecordObject(obj, name);
            return this;
        }

        /// <summary>
        /// BeginChangeCheck 
        /// </summary>
        /// <returns></returns>
        public UndoObject CheckChange()
        {
            EditorGUI.BeginChangeCheck();
            calledCheck = true;
            return this;
        }

        /// <summary>
        /// Record object undo
        /// </summary>
        /// <param name="obj">object to record</param>
        /// <param name="name">undo name</param>
        /// <returns></returns>
        public static IDisposable Record(Object obj, string name)
        {
            Undo.RecordObject(obj, name);
            return new UndoObject(obj);
        }

        /// <summary>
        /// If GUI check has been called earlier in the scope and result is true, Mark object as dirty. If GUI check hasn't been called, mark object as dirty no matter what the check result is.
        /// </summary>
        void IDisposable.Dispose()
        {
            EditorUtility.SetDirty(obj);
            calledCheck = false;
        }

        /// <summary>
        /// Check GUI changed, if true, record undo
        /// </summary>
        /// <param name="name">undo name</param>
        /// <returns></returns>
        public bool TryRecord(string name)
        {
            if (!calledCheck)
                throw new ArgumentException("CheckChange hasn't been called yet");
            bool check = EditorGUI.EndChangeCheck();
            if (check)
                Record(name);
            return check;
        }

        public void Clear()
        {
            Undo.ClearUndo(obj);
        }
    }
}