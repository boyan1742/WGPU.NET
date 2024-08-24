using System;
using static WGPU.NET.Wgpu;

namespace WGPU.NET
{
    public class Instance : IDisposable
    {
        private InstanceImpl _impl;

        public Instance(InstanceBackend backend = InstanceBackend.All, 
            InstanceFlag flags = InstanceFlag.Default, 
            Dx12Compiler dx12Compiler = Dx12Compiler.Undefined,
            Gles3MinorVersion gles3MinorVersion = Gles3MinorVersion.Automatic,
            string dxilPath = "", string dxcPath = "")
        {
            WgpuStructChain chain = new WgpuStructChain().AddInstanceExtras(backend, flags, dx12Compiler, gles3MinorVersion, dxilPath, dxcPath);

            var descriptor = new InstanceDescriptor()
            {
                nextInChain = chain.GetPointer()
            };
            _impl = CreateInstance(descriptor);
        }

        public Instance()
        {
            _impl = CreateInstance(new InstanceDescriptor());
        }

        public Surface CreateSurfaceFromAndroidNativeWindow(IntPtr window, string label = default) =>
            new Surface(InstanceCreateSurface(_impl, new SurfaceDescriptor()
            {
                label = label,
                nextInChain = new WgpuStructChain()
                    .AddSurfaceDescriptorFromAndroidNativeWindow(window)
                    .GetPointer()
            }));

        public Surface CreateSurfaceFromCanvasHTMLSelector(string selector, string label = default) =>
            new Surface(InstanceCreateSurface(_impl, new SurfaceDescriptor()
            {
                label = label,
                nextInChain = new WgpuStructChain()
                    .AddSurfaceDescriptorFromCanvasHTMLSelector(selector)
                    .GetPointer()
            }));

        public Surface CreateSurfaceFromMetalLayer(IntPtr layer, string label = default) =>
            new Surface(InstanceCreateSurface(_impl, new SurfaceDescriptor()
            {
                label = label,
                nextInChain = new WgpuStructChain()
                    .AddSurfaceDescriptorFromMetalLayer(layer)
                    .GetPointer()
            }));

        public Surface CreateSurfaceFromWaylandSurface(IntPtr display, string label = default) =>
            new Surface(InstanceCreateSurface(_impl, new SurfaceDescriptor()
            {
                label = label,
                nextInChain = new WgpuStructChain()
                    .AddSurfaceDescriptorFromWaylandSurface(display)
                    .GetPointer()
            }));

        public Surface CreateSurfaceFromWindowsHWND(IntPtr hinstance, IntPtr hwnd, string label = default) =>
            new Surface(InstanceCreateSurface(_impl, new SurfaceDescriptor()
            {
                label = label,
                nextInChain = new WgpuStructChain()
                    .AddSurfaceDescriptorFromWindowsHWND(hinstance, hwnd)
                    .GetPointer()
            }));

        public Surface CreateSurfaceFromXcbWindow(IntPtr connection, uint window, string label = default) =>
            new Surface(InstanceCreateSurface(_impl, new SurfaceDescriptor()
            {
                label = label,
                nextInChain = new WgpuStructChain()
                    .AddSurfaceDescriptorFromXcbWindow(connection, window)
                    .GetPointer()
            }));

        public Surface CreateSurfaceFromXlibWindow(IntPtr display, uint window, string label = default) =>
            new Surface(InstanceCreateSurface(_impl, new SurfaceDescriptor()
            {
                label = label,
                nextInChain = new WgpuStructChain()
                    .AddSurfaceDescriptorFromXlibWindow(display, window)
                    .GetPointer()
            }));

        public void ProcessEvents() => InstanceProcessEvents(_impl);

        public void RequestAdapter(Surface compatibleSurface, PowerPreference powerPreference, bool forceFallbackAdapter, RequestAdapterCallback callback)
        {
            InstanceRequestAdapter(_impl,
                new RequestAdapterOptions()
                {
                    compatibleSurface = compatibleSurface.Impl,
                    powerPreference = powerPreference,
                    forceFallbackAdapter = forceFallbackAdapter ? 1u : 0u
                },
                (s, a, m, _) => callback(s, new Adapter(a), m), IntPtr.Zero);
        }
        
        [Obsolete("Wgpu deprecated function. Use Instance constructor with backend overload and RequestAdapter without backend instead.")]
        public void RequestAdapter(Surface compatibleSurface, PowerPreference powerPreference, bool forceFallbackAdapter, RequestAdapterCallback callback, BackendType backendType)
        {
            InstanceRequestAdapter(_impl,
                new RequestAdapterOptions()
                {
                    compatibleSurface = compatibleSurface.Impl,
                    powerPreference = powerPreference,
                    forceFallbackAdapter = forceFallbackAdapter ? 1u : 0u,
                    backendType = backendType
                },
                (s, a, m, _) => callback(s, new Adapter(a), m), IntPtr.Zero);
        }
        
        public ReadOnlySpan<Adapter> EnumerateAdapters(BackendType type) => EnumerateAdapters(type.ToInstanceBackend());

        public ReadOnlySpan<Adapter> EnumerateAdapters(InstanceBackend type)
        {
            var options = new InstanceEnumerateAdapterOptions
            {
                nextInChain = IntPtr.Zero,
                backends = (uint)type
            };

            ulong length;
            unsafe { length = InstanceEnumerateAdapters(_impl, options, ref *(AdapterImpl*)null); }

            if(length == 0)
                return ReadOnlySpan<Adapter>.Empty;
            
            var adapters = new AdapterImpl[length];
            InstanceEnumerateAdapters(_impl, options, ref adapters[0]);

            var ret = new Adapter[length];
            for (ulong i = 0; i < length; i++)
                ret[i] = new Adapter(adapters[i]);

            return ret;
        }

        public void Dispose()
        {
            InstanceRelease(_impl);
            _impl = default;
        }
    }

    public delegate void RequestAdapterCallback(RequestAdapterStatus status, Adapter adapter, string message);
}
