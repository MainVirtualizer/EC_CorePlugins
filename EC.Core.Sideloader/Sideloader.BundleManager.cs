﻿using Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

namespace EC.Core.Sideloader
{
    public static class BundleManager
    {
        public static Dictionary<string, List<LazyCustom<AssetBundle>>> Bundles = new Dictionary<string, List<LazyCustom<AssetBundle>>>();
        public static string DummyPath => "list/characustom/00.unity3d";
        private static long CABCounter = 0;

        public static string GenerateCAB()
        {
            StringBuilder sb = new StringBuilder("CAB-", 36);

            sb.Append(Interlocked.Increment(ref CABCounter).ToString("x32"));

            return sb.ToString();
        }

        public static void RandomizeCAB(byte[] assetBundleData)
        {
            string ascii = Encoding.ASCII.GetString(assetBundleData, 0, 256);

            int cabIndex = ascii.IndexOf("CAB-", StringComparison.Ordinal);

            if (cabIndex < 0)
                return;

            int endIndex = ascii.Substring(cabIndex).IndexOf('\0');

            if (endIndex > 36)
                return;

            string CAB = GenerateCAB().Substring(4);
            byte[] cabBytes = Encoding.ASCII.GetBytes(CAB);

            Buffer.BlockCopy(cabBytes, 36 - endIndex, assetBundleData, cabIndex + 4, endIndex - 4);
        }

        public static void AddBundleLoader(Func<AssetBundle> func, string path, out string warning)
        {
            warning = "";

            if (Bundles.TryGetValue(path, out var lazyList))
            {
                warning = $"Duplicate asset bundle detected! {path}";
                lazyList.Add(LazyCustom<AssetBundle>.Create(func));
            }
            else
                Bundles.Add(path, new List<LazyCustom<AssetBundle>>
                {
                    LazyCustom<AssetBundle>.Create(func)
                });
        }

        public static bool TryGetObjectFromName<T>(string name, string assetBundle, out T obj) where T : UnityEngine.Object
        {
            bool result = TryGetObjectFromName(name, assetBundle, typeof(T), out UnityEngine.Object tObj);

            obj = (T)tObj;

            return result;
        }

        public static bool TryGetObjectFromName(string name, string assetBundle, Type type, out UnityEngine.Object obj)
        {
            obj = null;

            if (Bundles.TryGetValue(assetBundle, out List<LazyCustom<AssetBundle>> lazyBundleList))
            {
                foreach (AssetBundle bundle in lazyBundleList)
                {
                    if (bundle.Contains(name))
                    {
                        obj = bundle.LoadAsset(name, type);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
