using System;
using System.Runtime.CompilerServices;

namespace WGPU.NET
{
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Wgpu.InstanceBackend ToInstanceBackend(this Wgpu.BackendType type) => type switch
        {
            Wgpu.BackendType.Undefined => Wgpu.InstanceBackend.All,
            Wgpu.BackendType.Null => Wgpu.InstanceBackend.All,
            Wgpu.BackendType.WebGPU => Wgpu.InstanceBackend.BrowserWebGPU,
            Wgpu.BackendType.D3D11 => Wgpu.InstanceBackend.DX11,
            Wgpu.BackendType.D3D12 => Wgpu.InstanceBackend.DX12,
            Wgpu.BackendType.Metal => Wgpu.InstanceBackend.Metal,
            Wgpu.BackendType.Vulkan => Wgpu.InstanceBackend.Vulkan,
            Wgpu.BackendType.OpenGL => Wgpu.InstanceBackend.GL,
            Wgpu.BackendType.OpenGLES => Wgpu.InstanceBackend.GL,
            Wgpu.BackendType.Force32 => Wgpu.InstanceBackend.Force32,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
