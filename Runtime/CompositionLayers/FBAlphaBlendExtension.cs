// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.XR.CompositionLayers;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace OpenXR.Extensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/Composition Layers/Extensions/FB Alpha Blend")]
    public class FBAlphaBlendExtension : CompositionLayerExtension
    {
        public override ExtensionTarget Target => ExtensionTarget.Layer;

        public enum BlendFactor
        {
            Zero = 0,
            One = 1,
            SrcAlpha = 2,
            OneMinusSrcAlpha = 3,
            DstAlpha = 4,
            OneMinusDstAlpha = 5,
        }

        [SerializeField]
        BlendFactor m_SrcFactorColor = BlendFactor.SrcAlpha;
        
        [SerializeField]
        BlendFactor m_DstFactorColor = BlendFactor.OneMinusSrcAlpha;

        [SerializeField]
        BlendFactor m_SrcFactorAlpha = BlendFactor.SrcAlpha;

        [SerializeField]
        BlendFactor m_DstFactorAlpha = BlendFactor.OneMinusSrcAlpha;

        NativeArray<XrCompositionLayerAlphaBlendFB> m_NativeArray;

        public BlendFactor SrcFactorColor
        {
            get => m_SrcFactorColor;
            set => m_SrcFactorColor = UpdateValue(m_SrcFactorColor, value);
        }

        public BlendFactor DstFactorColor
        {
            get => m_DstFactorColor;
            set => m_DstFactorColor = UpdateValue(m_DstFactorColor, value);
        }

        public BlendFactor SrcFactorAlpha
        {
            get => m_SrcFactorAlpha;
            set => m_SrcFactorAlpha = UpdateValue(m_DstFactorColor, value);
        }

        public BlendFactor DstFactorAlpha
        {
            get => m_DstFactorAlpha;
            set => m_DstFactorAlpha = UpdateValue(m_DstFactorColor, value);
        }

        public override unsafe void* GetNativeStructPtr()
        {
            var alphaBlendStruct = new XrCompositionLayerAlphaBlendFB()
            {
                type = XrStructureTypeExt.XR_TYPE_COMPOSITION_LAYER_ALPHA_BLEND_FB,
                next = null,
                srcFactorColor = (XrBlendFactorFB)m_SrcFactorColor,
                dstFactorColor = (XrBlendFactorFB)m_DstFactorColor,
                srcFactorAlpha = (XrBlendFactorFB)m_SrcFactorAlpha,
                dstFactorAlpha = (XrBlendFactorFB)m_DstFactorAlpha
            };

            if (!m_NativeArray.IsCreated)
                m_NativeArray = new NativeArray<XrCompositionLayerAlphaBlendFB>(1, Allocator.Persistent);

            m_NativeArray[0] = alphaBlendStruct;
            return m_NativeArray.GetUnsafePtr();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (m_NativeArray.IsCreated)
                m_NativeArray.Dispose();
        }
    }
}
