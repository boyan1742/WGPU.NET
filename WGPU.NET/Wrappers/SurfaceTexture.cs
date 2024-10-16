namespace WGPU.NET
{
    public class SurfaceTexture
    {
        public Texture Texture { get; }

        public uint Suboptimal { get; }

        public Wgpu.SurfaceGetCurrentTextureStatus Status { get; }

        internal SurfaceTexture(Texture texture, uint suboptimal, Wgpu.SurfaceGetCurrentTextureStatus status)
        {
            Texture = texture;
            Suboptimal = suboptimal;
            Status = status;
        }

        public TextureView GetTextureView()
        {
            return Texture.CreateTextureView("Surface Texture", 
                Wgpu.TextureGetFormat(Texture.Impl), Wgpu.TextureViewDimension.TwoDimensions, 
                0, 1, 0, 1, Wgpu.TextureAspect.All);
        }
    }
}
