using System;
using static WGPU.NET.Wgpu;

namespace WGPU.NET
{
    public class Surface : IDisposable
    {
        internal SurfaceImpl Impl;

        internal Surface(SurfaceImpl impl)
        {
            if (impl.Handle == IntPtr.Zero)
                throw new ResourceCreationError(nameof(Surface));

            Impl = impl;
        }

        public TextureFormat GetPreferredFormat(Adapter adapter)
            => GetCapabilities(adapter).formats[0];

        public SurfaceTexture GetCurrentTexture()
        {
            var txt = new Wgpu.SurfaceTexture();
            SurfaceGetCurrentTexture(Impl, ref txt);

            TextureDescriptor desc = new TextureDescriptor()
            {
                label = "",
                nextInChain = IntPtr.Zero,
                dimension = TextureGetDimension(txt.texture),
                format = TextureGetFormat(txt.texture),
                size = new Extent3D
                {
                    height = TextureGetHeight(txt.texture),
                    width = TextureGetWidth(txt.texture),
                    depthOrArrayLayers = TextureGetDepthOrArrayLayers(txt.texture)
                },
                usage = TextureGetUsage(txt.texture),
                sampleCount = TextureGetSampleCount(txt.texture),
                mipLevelCount = TextureGetMipLevelCount(txt.texture),
                viewFormats = IntPtr.Zero,
                viewFormatCount = 0
            };

            SurfaceTexture texture = new SurfaceTexture
            (
                new Texture(txt.texture, desc),
                txt.suboptimal,
                txt.status
            );

            return texture;
        }

        public TextureView GetCurrentTextureView() => GetCurrentTexture().GetTextureView();

        public void Configure(Device device, SurfaceConfiguration config)
        {
            var surfaceConfiguration = new Wgpu.SurfaceConfiguration
            {
                device = device.Impl,
                presentMode = config.presentMode,
                nextInChain = config.nextInChain,
                format = config.format,
                height = config.height,
                width = config.width,
                usage = (uint)config.usage,
                alphaMode = config.alphaMode
            };

            if (config.viewFormats is { } && config.viewFormats.Length > 0)
                unsafe
                {
                    fixed (TextureFormat* tf = &config.viewFormats[0])
                    {
                        surfaceConfiguration.viewFormats = new IntPtr(tf);
                        surfaceConfiguration.viewFormatCount = (ulong)config.viewFormats.Length;
                    }
                }

            SurfaceConfigure(Impl, surfaceConfiguration);
        }

        public void Unconfigure() => SurfaceUnconfigure(Impl);

        public void Present() => SurfacePresent(Impl);

        public SurfaceCapabilities GetCapabilities(Adapter adapter)
        {
            Wgpu.SurfaceCapabilities capabilities = new Wgpu.SurfaceCapabilities();
            SurfaceGetCapabilities(Impl, adapter.Impl, ref capabilities);

            SurfaceCapabilities caps = new SurfaceCapabilities
            {
                nextInChain = capabilities.nextInChain,
                usages = capabilities.usages,
                formats = new TextureFormat[capabilities.formatCount],
                presentModes = new PresentMode[capabilities.presentModeCount],
                alphaModes = new CompositeAlphaMode[capabilities.alphaModeCount]
            };

            TextureFormat[] formatsTemp;
            PresentMode[] presentModesTemp;
            CompositeAlphaMode[] alphaModesTemp;

            unsafe
            {
                formatsTemp = new ReadOnlySpan<TextureFormat>(capabilities.formats.ToPointer(),
                    (int)capabilities.formatCount).ToArray();

                presentModesTemp = new ReadOnlySpan<PresentMode>(capabilities.presentModes.ToPointer(),
                    (int)capabilities.presentModeCount).ToArray();

                alphaModesTemp = new ReadOnlySpan<CompositeAlphaMode>(capabilities.alphaModes.ToPointer(),
                    (int)capabilities.alphaModeCount).ToArray();
            }

            Array.Copy(formatsTemp, caps.formats, caps.formats.Length);
            Array.Copy(presentModesTemp, caps.presentModes, caps.presentModes.Length);
            Array.Copy(alphaModesTemp, caps.alphaModes, caps.alphaModes.Length);

            formatsTemp = null;
            presentModesTemp = null;
            alphaModesTemp = null;

            SurfaceCapabilitiesFreeMembers(capabilities);

            return caps;
        }

        public void Dispose()
        {
            SurfaceRelease(Impl);
            Impl = default;
        }
    }
}
