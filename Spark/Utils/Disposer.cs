﻿using System;

namespace Spark
{
    public static class Disposer
    {
        /// <summary>
        /// Dispose an object instance and set the reference to null
        /// </summary>
        /// <typeparam name="TypeName">The type of object to dispose</typeparam>
        /// <param name="resource">A reference to the instance for disposal</param>
        /// <remarks>This method hides any thrown exceptions that might occur during disposal of the object (by design)</remarks>
        public static void SafeDispose<T>(ref T resource) where T : class
        {
            if (resource is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch { }
            }

            resource = null;
        }
    }
}