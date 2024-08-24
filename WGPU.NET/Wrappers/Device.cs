using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static WGPU.NET.Wgpu;

namespace WGPU.NET
{
    public struct BindGroupEntry
    {
        public uint Binding;
        public Buffer Buffer;
        public ulong Offset;
        public ulong Size;
        public Sampler Sampler;
        public TextureView TextureView;
    }

    public struct ProgrammableStageDescriptor
    {
        public ShaderModule Module;
        public string EntryPoint;
    }

    public struct VertexState
    {
        public ShaderModule Module;
        public string EntryPoint;
        public VertexBufferLayout[] bufferLayouts;
    }

    public partial struct VertexBufferLayout
    {
        public ulong ArrayStride;

        public Wgpu.VertexStepMode StepMode;

        public VertexAttribute[] Attributes;
    }

    public struct FragmentState
    {
        public ShaderModule Module;
        public string EntryPoint;
        public ColorTargetState[] colorTargets;
    }

    public struct ColorTargetState
    {
        public Wgpu.TextureFormat Format;

        public BlendState? BlendState;

        public uint WriteMask;
    }

    public class Device : IDisposable
    {
        private DeviceImpl _impl;

        internal DeviceImpl Impl
        {
            get
            {
                if (_impl.Handle == IntPtr.Zero)
                    throw new HandleDroppedOrDestroyedException(nameof(Device));

                return _impl;
            }

            private set => _impl = value;
        }

        public Queue Queue { get; private set; }

        internal Device(DeviceImpl impl)
        {
            if (impl.Handle == IntPtr.Zero)
                throw new ResourceCreationError(nameof(Device));

            Impl = impl;
            Queue = new Queue(DeviceGetQueue(impl));
        }

        public void PollDevice(uint wait)
        {
            DevicePoll(_impl, wait, new WrappedSubmissionIndex
            {
                queue = Queue.Impl,
                submissionIndex = 0
            });
        }

        public BindGroup CreateBindGroup(string label, BindGroupLayout layout, BindGroupEntry[] entries)
        {
            Span<Wgpu.BindGroupEntry> entriesInner = stackalloc Wgpu.BindGroupEntry[entries.Length];

            for (int i = 0; i < entries.Length; i++)
            {
                BindGroupEntry entryOuter = entries[i];
                entriesInner[i] = new Wgpu.BindGroupEntry
                {
                    binding = entryOuter.Binding,
                    buffer = entryOuter.Buffer?.Impl ?? default,
                    offset = entryOuter.Offset,
                    size = entryOuter.Size,
                    sampler = entryOuter.Sampler?.Impl ?? default,
                    textureView = entryOuter.TextureView?.Impl ?? default
                };
            }

            unsafe
            {
                return new BindGroup(
                    DeviceCreateBindGroup(Impl, new BindGroupDescriptor
                    {
                        label = label,
                        layout = layout.Impl,
                        entries = new IntPtr(Unsafe.AsPointer(ref entriesInner.GetPinnableReference())),
                        entryCount = (uint)entries.Length
                    })
                );
            }
        }

        public BindGroupLayout CreateBindgroupLayout(string label, BindGroupLayoutEntry[] entries)
        {
            unsafe
            {
                fixed (BindGroupLayoutEntry* entriesPtr = entries)
                {
                    return BindGroupLayout.For(
                        DeviceCreateBindGroupLayout(Impl, new BindGroupLayoutDescriptor
                        {
                            label = label,
                            entries = new IntPtr(entriesPtr),
                            entryCount = (uint)entries.Length
                        })
                    );
                }
            }
        }

        public Buffer CreateBuffer(string label, bool mappedAtCreation, ulong size, BufferUsage usage)
        {
            var desc = new BufferDescriptor
            {
                label = label,
                mappedAtCreation = mappedAtCreation ? 1u : 0u,
                size = size,
                usage = (uint)usage
            };

            return new Buffer(DeviceCreateBuffer(Impl, desc), desc);
        }

        public CommandEncoder CreateCommandEncoder(string label) =>
            new CommandEncoder(
                DeviceCreateCommandEncoder(Impl, new CommandEncoderDescriptor
                {
                    label = label
                })
            );

        public ComputePipeline CreateComputePipeline(string label, ProgrammableStageDescriptor compute) =>
            new ComputePipeline(
                DeviceCreateComputePipeline(Impl, new ComputePipelineDescriptor
                {
                    label = label,
                    compute = new Wgpu.ProgrammableStageDescriptor
                    {
                        module = compute.Module.Impl,
                        entryPoint = compute.EntryPoint
                    }
                })
            );

        public void CreateComputePipelineAsync(string label, CreateComputePipelineAsyncCallback callback,
            ProgrammableStageDescriptor compute)
        {
            DeviceCreateComputePipelineAsync(Impl, new ComputePipelineDescriptor
                {
                    label = label,
                    compute = new Wgpu.ProgrammableStageDescriptor
                    {
                        module = compute.Module.Impl,
                        entryPoint = compute.EntryPoint
                    }
                }, (s, p, m, _) => callback(s, new ComputePipeline(p), m), IntPtr.Zero
            );
        }

        public delegate void CreateComputePipelineAsyncCallback(CreatePipelineAsyncStatus status, ComputePipeline pipeline,
            string message);

        public PipelineLayout CreatePipelineLayout(string label, BindGroupLayout[] bindGroupLayouts)
        {
            Span<BindGroupLayoutImpl> bindGroupLayoutsInner = stackalloc BindGroupLayoutImpl[bindGroupLayouts.Length];

            for (int i = 0; i < bindGroupLayouts.Length; i++)
                bindGroupLayoutsInner[i] = bindGroupLayouts[i].Impl;

            unsafe
            {
                return new PipelineLayout(
                    DeviceCreatePipelineLayout(Impl, new PipelineLayoutDescriptor
                    {
                        label = label,
                        bindGroupLayouts = new IntPtr(Unsafe.AsPointer(ref bindGroupLayoutsInner.GetPinnableReference())),
                        bindGroupLayoutCount = (uint)bindGroupLayouts.Length
                    })
                );
            }
        }

        public QuerySet CreateQuerySet(string label, QueryType queryType, uint count, PipelineStatisticName[] pipelineStatistics)
        {
            unsafe
            {
                fixed (PipelineStatisticName* pipelineStatisticsPtr = pipelineStatistics)
                {
                    return new QuerySet(
                        DeviceCreateQuerySet(Impl, new QuerySetDescriptor
                        {
                            label = label,
                            type = queryType,
                            count = count,
                            //pipelineStatistics = new IntPtr(pipelineStatisticsPtr),
                            //pipelineStatisticsCount = (uint)pipelineStatistics.Length
                        })
                    );
                }
            }
        }

        public RenderBundleEncoder CreateRenderBundleEncoder(string label, TextureFormat[] colorFormats,
            TextureFormat depthStencilFormat,
            uint sampleCount, bool depthReadOnly, bool stencilReadOnly)
        {
            unsafe
            {
                fixed (TextureFormat* colorFormatsPtr = colorFormats)
                {
                    return new RenderBundleEncoder(
                        DeviceCreateRenderBundleEncoder(Impl, new RenderBundleEncoderDescriptor
                        {
                            label = label,
                            colorFormats = new IntPtr(colorFormatsPtr),
                            colorFormatCount = (ulong)colorFormats.LongLength,
                            depthStencilFormat = depthStencilFormat,
                            sampleCount = sampleCount,
                            depthReadOnly = depthReadOnly ? 1u : 0u,
                            stencilReadOnly = stencilReadOnly ? 1u : 0u
                        })
                    );
                }
            }
        }

        public RenderPipeline CreateRenderPipeline(string label, PipelineLayout layout,
            VertexState vertexState, PrimitiveState primitiveState, MultisampleState multisampleState,
            DepthStencilState? depthStencilState = null, FragmentState? fragmentState = null)
        {
            RenderPipelineDescriptor desc = CreateRenderPipelineDescriptor(label, layout, vertexState, primitiveState,
                multisampleState, depthStencilState, fragmentState);
            RenderPipelineImpl pipelineImpl = DeviceCreateRenderPipeline(Impl, desc);

            FreeRenderPipelineDescriptor(desc);

            return new RenderPipeline(pipelineImpl);
        }

        public void CreateRenderPipelineAsync(string label, CreateRenderPipelineAsyncCallback callback, PipelineLayout layout,
            VertexState vertexState, PrimitiveState primitiveState, MultisampleState multisampleState,
            DepthStencilState? depthStencilState = null, FragmentState? fragmentState = null)
        {
            RenderPipelineDescriptor desc = CreateRenderPipelineDescriptor(label, layout, vertexState, primitiveState,
                multisampleState, depthStencilState, fragmentState);
            DeviceCreateRenderPipelineAsync(Impl, desc, (s, p, m, _) =>
            {
                FreeRenderPipelineDescriptor(desc);
                callback(s, new RenderPipeline(p), m);
            }, IntPtr.Zero);
        }

        public delegate void CreateRenderPipelineAsyncCallback(CreatePipelineAsyncStatus status, RenderPipeline pipeline,
            string message);

        private static RenderPipelineDescriptor CreateRenderPipelineDescriptor(string label, PipelineLayout layout,
            VertexState vertexState,
            PrimitiveState primitiveState, MultisampleState multisampleState, DepthStencilState? depthStencilState,
            FragmentState? fragmentState) =>
            new RenderPipelineDescriptor
            {
                label = label,
                layout = layout.Impl,
                vertex = new Wgpu.VertexState
                {
                    module = vertexState.Module.Impl,
                    entryPoint = vertexState.EntryPoint,
                    buffers = Util.AllocHArray(vertexState.bufferLayouts.Length,
                        vertexState.bufferLayouts.Select(x => new Wgpu.VertexBufferLayout
                        {
                            arrayStride = x.ArrayStride,
                            stepMode = x.StepMode,
                            attributes = Util.AllocHArray(x.Attributes),
                            attributeCount = (uint)x.Attributes.Length
                        })
                    ),
                    bufferCount = (uint)vertexState.bufferLayouts.Length
                },
                primitive = primitiveState,
                depthStencil = Util.Optional(depthStencilState),
                multisample = multisampleState,
                fragment = fragmentState == null
                    ? IntPtr.Zero
                    : Util.AllocHStruct(new Wgpu.FragmentState(module: fragmentState.Value.Module.Impl,
                        entryPoint: fragmentState.Value.EntryPoint, fragmentState.Value.colorTargets.Length == 0
                            ? IntPtr.Zero
                            : Util.AllocHArray(fragmentState.Value.colorTargets.Length, fragmentState.Value.colorTargets.Select(
                                x =>
                                    new Wgpu.ColorTargetState
                                    {
                                        format = x.Format,
                                        blend = Util.Optional(x.BlendState),
                                        writeMask = x.WriteMask
                                    })),
                        targetCount: (uint)fragmentState.Value.colorTargets.Length))
            };

        private static void FreeRenderPipelineDescriptor(RenderPipelineDescriptor descriptor)
        {
            unsafe
            {
                Wgpu.VertexBufferLayout* buffers = (Wgpu.VertexBufferLayout*)descriptor.vertex.buffers;

                for (ulong i = 0; i < descriptor.vertex.bufferCount; i++)
                    Util.FreePtr(buffers[i].attributes);

                Util.FreePtr(descriptor.vertex.buffers);
                Util.FreePtr(descriptor.depthStencil);

                if (descriptor.fragment == IntPtr.Zero)
                    return;

                var fragment = (Wgpu.FragmentState)Marshal.PtrToStructure(descriptor.fragment, typeof(Wgpu.FragmentState));
                if (fragment.targetCount > 0)
                {
                    for (ulong i = 0; i < fragment.targetCount; i++)
                    {
                        var colorTarget = (Wgpu.ColorTargetState)Marshal.PtrToStructure(
                            fragment.targets + (int)(i * (ulong)sizeof(Wgpu.ColorTargetState)), typeof(Wgpu.ColorTargetState));
                        Util.FreePtr(colorTarget.blend);
                    }

                    Util.FreePtr(fragment.targets);
                }

                Util.FreePtr(descriptor.fragment);
            }
        }

        public Sampler CreateSampler(string label, AddressMode addressModeU, AddressMode addressModeV, AddressMode addressModeW,
            FilterMode magFilter, FilterMode minFilter, MipmapFilterMode mipmapFilter,
            float lodMinClamp, float lodMaxClamp, CompareFunction compare, ushort maxAnisotropy) =>
            new Sampler(
                DeviceCreateSampler(Impl, new SamplerDescriptor
                {
                    label = label,
                    addressModeU = addressModeU,
                    addressModeV = addressModeV,
                    addressModeW = addressModeW,
                    magFilter = magFilter,
                    minFilter = minFilter,
                    mipmapFilter = mipmapFilter,
                    lodMinClamp = lodMinClamp,
                    lodMaxClamp = lodMaxClamp,
                    compare = compare,
                    maxAnisotropy = maxAnisotropy
                })
            );

        public ShaderModule CreateSprivShaderModule(string label, byte[] spirvCode) =>
            new ShaderModule(
                DeviceCreateShaderModule(Impl, new ShaderModuleDescriptor
                {
                    label = label,
                    nextInChain = new WgpuStructChain()
                        .AddShaderModuleSPIRVDescriptor(spirvCode)
                        .GetPointer()
                })
            );

        public ShaderModule CreateWgslShaderModule(string label, string wgslCode) =>
            new ShaderModule(
                DeviceCreateShaderModule(Impl, new ShaderModuleDescriptor
                {
                    label = label,
                    nextInChain = new WgpuStructChain()
                        .AddShaderModuleWGSLDescriptor(wgslCode)
                        .GetPointer()
                })
            );

        public Texture CreateTexture(string label, TextureUsage usage,
            TextureDimension dimension, Extent3D size, TextureFormat format,
            uint mipLevelCount, uint sampleCount)
        {
            var desc = new TextureDescriptor
            {
                label = label,
                usage = (uint)usage,
                dimension = dimension,
                size = size,
                format = format,
                mipLevelCount = mipLevelCount,
                sampleCount = sampleCount,
            };

            return CreateTexture(in desc);
        }

        public Texture CreateTexture(in TextureDescriptor descriptor) =>
            new Texture(DeviceCreateTexture(Impl, descriptor), descriptor);

        public FeatureName[] EnumerateFeatures()
        {
            FeatureName temp = default;

            ulong size = DeviceEnumerateFeatures(Impl, ref temp);

            var features = new FeatureName[size];

            DeviceEnumerateFeatures(Impl, ref features[0]);

            return features;
        }

        public bool GetLimits(out SupportedLimits limits)
        {
            limits = new SupportedLimits();

            return DeviceGetLimits(Impl, ref limits) == 1;
        }

        public bool HasFeature(FeatureName feature) => DeviceHasFeature(Impl, feature) == 1;

        public void PushErrorScope(ErrorFilter filter) => DevicePushErrorScope(Impl, filter);

        public void PopErrorScope(ErrorCallback callback)
        {
            DevicePopErrorScope(Impl,
                (t, m, _) => callback(t, m),
                IntPtr.Zero);
        }

        private static readonly List<Wgpu.ErrorCallback> s_errorCallbacks =
            new List<Wgpu.ErrorCallback>();

        public void SetUncapturedErrorCallback(ErrorCallback callback)
        {
            Wgpu.ErrorCallback errorCallback = (t, m, _) => callback(t, m);

            s_errorCallbacks.Add(errorCallback);

            DeviceSetUncapturedErrorCallback(Impl,
                errorCallback,
                IntPtr.Zero);
        }

        public void Dispose()
        {
            Queue.Dispose();
            Queue = null;

            DeviceDestroy(Impl);
            DeviceRelease(Impl);
            Impl = default;
        }
    }

    public delegate void ErrorCallback(ErrorType type, string message);
    public delegate void DeviceLostCallback(DeviceLostReason reason, string message);
}
