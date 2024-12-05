// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using UnityEngine.XR.OpenXR.Features;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace OpenXR.Extensions
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Android Settings",
        BuildTargetGroups = new [] { BuildTargetGroup.Android },
        Company = "Mikesky",
        Desc = "Additional Android settings for OpenXR.",
        OpenxrExtensionStrings = "",
        Version = "0.1.0",
        FeatureId = FeatureId)]
#endif
    public class OpenXRAndroidSettings : FeatureBase<OpenXRAndroidSettings>
    {
        public const string FeatureId = "dev.mikesky.openxr.extensions.androidmanifest";

        /// <summary>
        /// Uses a PNG in the Assets folder as the system splash screen image. If set, the OS will display the system splash screen as a high quality compositor layer as soon as the app is starting to launch until the app submits the first frame.
        /// </summary>
        [SerializeField, Tooltip("Uses a PNG in the Assets folder as the system splash screen image. If set, the OS will display the system splash screen as a high quality compositor layer as soon as the app is starting to launch until the app submits the first frame.")]
        public Texture2D systemSplashScreen;

        protected override bool CheckRequiredExtensions()
        {
            return true;
        }
    }
}
