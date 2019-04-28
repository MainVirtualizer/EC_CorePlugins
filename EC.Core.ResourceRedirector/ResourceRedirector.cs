﻿using BepInEx;
using BepInEx.Logging;
using EC.Core.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EC.Core.ResourceRedirector
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class ResourceRedirector : BaseUnityPlugin
    {
        public const string PluginName = "Resource Redirector";
        public const string GUID = "EC.Core.ResourceRedirector";
        public const string Version = Metadata.PluginsVersion;

        internal static string EmulatedDir => Path.Combine(Paths.GameRootPath, "abdata-emulated");
        internal static bool EmulationEnabled;

        public delegate bool AssetHandler(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, out AssetBundleLoadAssetOperation result);
        public delegate bool AssetBundleHandler(string assetBundleName, out AssetBundle result);

        public static List<AssetHandler> AssetResolvers = new List<AssetHandler>();
        public static List<AssetBundleHandler> AssetBundleResolvers = new List<AssetBundleHandler>();

        public static Dictionary<string, AssetBundle> EmulatedAssetBundles = new Dictionary<string, AssetBundle>();
        internal static new ManualLogSource Logger;

        public ResourceRedirector()
        {
            Logger = base.Logger;
            Hooks.InstallHooks();

            EmulationEnabled = Directory.Exists(EmulatedDir);
        }

        public static AssetBundleLoadAssetOperation HandleAsset(string assetBundleName, string assetName, Type type, string manifestAssetBundleName, ref AssetBundleLoadAssetOperation __result)
        {
            foreach (var handler in AssetResolvers)
            {
                try
                {
                    if (handler.Invoke(assetBundleName, assetName, type, manifestAssetBundleName, out AssetBundleLoadAssetOperation result))
                        return result;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, ex.ToString());
                }
            }

            //emulate asset load
            string dir = Path.Combine(EmulatedDir, assetBundleName.Replace('/', '\\').Replace(".unity3d", ""));

            if (Directory.Exists(dir))
            {
                if (type == typeof(Texture2D))
                {
                    string path = Path.Combine(dir, $"{assetName}.png");

                    if (!File.Exists(path))
                        return __result;

                    Logger.Log(LogLevel.Debug, $"Loading emulated asset {path}");

                    var tex = AssetLoader.LoadTexture(path);

                    if (path.Contains("clamp"))
                        tex.wrapMode = TextureWrapMode.Clamp;
                    else if (path.Contains("repeat"))
                        tex.wrapMode = TextureWrapMode.Repeat;


                    return new AssetBundleLoadAssetOperationSimulation(tex);
                }

                if (type == typeof(AudioClip) || type == typeof(UnityEngine.Object) && assetBundleName.StartsWith("sound", StringComparison.Ordinal))
                {
                    string path = Path.Combine(dir, $"{assetName}.wav");

                    if (!File.Exists(path))
                        return __result;

                    Logger.Log(LogLevel.Debug, $"Loading emulated asset {path}");

                    return new AssetBundleLoadAssetOperationSimulation(AssetLoader.LoadAudioClip(path, AudioType.WAV));
                }
            }

            string emulatedPath = Path.Combine(EmulatedDir, assetBundleName.Replace('/', '\\'));

            if (File.Exists(emulatedPath))
            {
                if (!EmulatedAssetBundles.TryGetValue(emulatedPath, out AssetBundle bundle))
                {
                    bundle = AssetBundle.LoadFromFile(emulatedPath);

                    EmulatedAssetBundles[emulatedPath] = bundle;
                }

                return new AssetBundleLoadAssetOperationSimulation(bundle.LoadAsset(assetName));
            }

            //otherwise return normal asset
            return __result;
        }

        public static AssetBundle HandleAssetBundle(string assetBundleName)
        {
            foreach (var handler in AssetBundleResolvers)
            {
                try
                {
                    if (handler.Invoke(assetBundleName, out AssetBundle result))
                        return result;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, ex.ToString());
                }
            }

            //otherwise load the asset bundle
            return AssetBundle.LoadFromFile(assetBundleName);
        }
        /// <summary>
        /// Check if the asset bundle file exists on disk. Moved to a separate method so other plugins can hook and override if necessary.
        /// </summary>
        public static bool AssetBundleExists(string assetBundleName) => File.Exists($"{Paths.GameRootPath}/abdata/{assetBundleName}");
    }
}
