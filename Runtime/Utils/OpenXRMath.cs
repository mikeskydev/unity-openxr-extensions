// SPDX-FileCopyrightText: 2024 Michael Nisbet <me@mikesky.dev>
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace OpenXR.Extensions
{
    public static class OpenXRMath
    {
        public static Vector3 AsUnityVector3(this XrVector3f position)
        {
            return new Vector3(position.X, position.Y, -position.Z);
        }

        public static Quaternion AsUnityQuaternion(this XrQuaternionf rotation)
        {
            return new Quaternion(-rotation.X, -rotation.Y, rotation.Z, rotation.W);
        }
    }
}
