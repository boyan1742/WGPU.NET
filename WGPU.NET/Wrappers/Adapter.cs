﻿using System;
using System.Runtime.CompilerServices;
using static WGPU.NET.Wgpu;

namespace WGPU.NET
{
    public struct RequiredLimits
    {
        public Limits Limits;
    }

    public partial struct RequiredLimitsExtras
    {
        public uint MaxPushConstantSize;
    }

    public struct DeviceExtras
    {
        public string TracePath;
    }

    public class Adapter : IDisposable
    {
        internal AdapterImpl Impl;

        internal Adapter(AdapterImpl impl)
        {
            if (impl.Handle == IntPtr.Zero)
                throw new ResourceCreationError(nameof(Adapter));

            Impl = impl;
        }


        public unsafe FeatureName[] EnumerateFeatures()
        {
            FeatureName features = default;

            ulong size = AdapterEnumerateFeatures(Impl, ref features);

            var featuresSpan = new Span<FeatureName>(Unsafe.AsPointer(ref features), (int)size);

            FeatureName[] result = new FeatureName[size];

            featuresSpan.CopyTo(result);

            return result;
        }

        public bool GetLimits(out SupportedLimits limits)
        {
            limits = new SupportedLimits();

            return AdapterGetLimits(Impl, ref limits) == 1;
        }

        public void GetProperties(out AdapterProperties properties)
        {
            properties = new AdapterProperties();

            AdapterGetProperties(Impl, ref properties);
        }

        public bool HasFeature(FeatureName feature) => AdapterHasFeature(Impl, feature) == 1;

        public void RequestDevice(RequestDeviceCallback callback, string label, NativeFeature[] nativeFeatures, QueueDescriptor defaultQueue = default, 
            Limits? limits = null, RequiredLimitsExtras? limitsExtras = null, DeviceExtras? deviceExtras = null, DeviceLostCallback deviceLostCallback = null)
        {
            Wgpu.RequiredLimits requiredLimits = default;
            WgpuStructChain limitsExtrasChain = null;
            WgpuStructChain deviceExtrasChain = null;

            if (limitsExtras != null)
            {
                limitsExtrasChain = new WgpuStructChain()
                    .AddRequiredLimitsExtras(
                        limitsExtras.Value.MaxPushConstantSize);
            }

            if (limits != null)
            {
                requiredLimits = new Wgpu.RequiredLimits
                {
                    nextInChain = limitsExtras == null
                        ? IntPtr.Zero
                        : limitsExtrasChain.GetPointer(),
                    limits = limits.Value
                };
            }

            if (deviceExtras != null)
                deviceExtrasChain = new WgpuStructChain().AddDeviceExtras(deviceExtras.Value.TracePath);

            unsafe
            {
                fixed (NativeFeature* requiredFeatures = nativeFeatures)
                {
                    AdapterRequestDevice(Impl, new DeviceDescriptor()
                        {
                            defaultQueue = defaultQueue,
                            requiredLimits = limits != null ? new IntPtr(&requiredLimits) : IntPtr.Zero,
                            requiredFeatureCount = (ulong)nativeFeatures.LongLength,
                            requiredFeatures = new IntPtr(requiredFeatures),
                            label = label,
                            deviceLostCallback = (reason, message, _) => deviceLostCallback?.Invoke(reason, message),
                            nextInChain = deviceExtras==null ? IntPtr.Zero : deviceExtrasChain.GetPointer()
                        }, 
                        (s,d,m,_) => callback(s,new Device(d),m), IntPtr.Zero);
                }
            }
            
            limitsExtrasChain?.Dispose();
            deviceExtrasChain?.Dispose();
        }

        public void Dispose() => AdapterRelease(Impl);
    }

    public delegate void RequestDeviceCallback(RequestDeviceStatus status, Device device, string message);
}
