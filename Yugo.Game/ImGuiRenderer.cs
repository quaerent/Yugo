using System.Reflection;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Yugo.Game.Screens;

namespace Yugo.Game;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ImGuiVertex : IVertexType
{
    public System.Numerics.Vector2 Pos;
    public System.Numerics.Vector2 Tex;
    public uint Color;

    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}

public class ImGuiRenderer
{
    private readonly Microsoft.Xna.Framework.Game _game;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private readonly RasterizerState _rasterizerState;

    private Texture2D _fontTexture = null!;
    private DynamicVertexBuffer? _vertexBuffer;
    private DynamicIndexBuffer? _indexBuffer;

    private byte[] _vertexData = Array.Empty<byte>();
    private byte[] _indexData = Array.Empty<byte>();

    private int _vertexBufferSize;
    private int _indexBufferSize;
    private int _prevMouseWheel;

    public ImGuiRenderer(Microsoft.Xna.Framework.Game game)
    {
        _game = game;
        _graphicsDevice = game.GraphicsDevice;

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        ImGui.StyleColorsLight();

        // Ensure we have at least one font
        ImGui.GetIO().Fonts.AddFontDefault();

        _rasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            DepthClipEnable = false,
            ScissorTestEnable = true,
        };

        _effect = new BasicEffect(_graphicsDevice)
        {
            World = Matrix.Identity,
            View = Matrix.Identity,
            TextureEnabled = true,
            VertexColorEnabled = true,
        };

        _game.Window.TextInput += (s, e) =>
        {
            if (e.Character != '\t')
                ImGui.GetIO().AddInputCharacter(e.Character);
        };

        RebuildFontAtlas();
    }

    public void RebuildFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(
            out IntPtr pixels,
            out int width,
            out int height,
            out int bytesPerPixel
        );

        var pixelData = new byte[width * height * bytesPerPixel];
        Marshal.Copy(pixels, pixelData, 0, pixelData.Length);

        if (_fontTexture != null)
            _fontTexture.Dispose();
        _fontTexture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
        _fontTexture.SetData(pixelData);

        io.Fonts.SetTexID((IntPtr)1);
        io.Fonts.ClearTexData();
    }

    public void BeforeLayout(GameTime gameTime)
    {
        var io = ImGui.GetIO();
        io.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        io.DisplaySize = new System.Numerics.Vector2(
            _graphicsDevice.Viewport.Width,
            _graphicsDevice.Viewport.Height
        );

        UpdateInput();
        ImGui.NewFrame();
    }

    public void AfterLayout()
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    private void UpdateInput()
    {
        var io = ImGui.GetIO();
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        var viewport = _graphicsDevice.Viewport;

        bool isInside =
            mouse.X >= 0 && mouse.Y >= 0 && mouse.X < viewport.Width && mouse.Y < viewport.Height;
        bool isFocused = isInside && _game.IsActive;

        if (isFocused)
        {
            io.MousePos = new System.Numerics.Vector2(mouse.X, mouse.Y);
            io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed;
            io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed;
            io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed;

            float scrollDelta = (mouse.ScrollWheelValue - _prevMouseWheel) / 120f;
            io.AddMouseWheelEvent(0, scrollDelta);
        }
        else
        {
            io.MouseDown[0] = false;
            io.MouseDown[1] = false;
            io.MouseDown[2] = false;
            io.MousePos = new System.Numerics.Vector2(-1, -1);
        }

        _prevMouseWheel = mouse.ScrollWheelValue;

        io.AddKeyEvent(
            ImGuiKey.ModCtrl,
            keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl)
        );
        io.AddKeyEvent(
            ImGuiKey.ModShift,
            keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift)
        );
        io.AddKeyEvent(
            ImGuiKey.ModAlt,
            keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt)
        );

        MapKey(io, ImGuiKey.Backspace, Keys.Back, keyboard);
        MapKey(io, ImGuiKey.Enter, Keys.Enter, keyboard);
        MapKey(io, ImGuiKey.Tab, Keys.Tab, keyboard);
        MapKey(io, ImGuiKey.LeftArrow, Keys.Left, keyboard);
        MapKey(io, ImGuiKey.RightArrow, Keys.Right, keyboard);
        MapKey(io, ImGuiKey.UpArrow, Keys.Up, keyboard);
        MapKey(io, ImGuiKey.DownArrow, Keys.Down, keyboard);
        MapKey(io, ImGuiKey.Delete, Keys.Delete, keyboard);
        MapKey(io, ImGuiKey.Home, Keys.Home, keyboard);
        MapKey(io, ImGuiKey.End, Keys.End, keyboard);
    }

    private void MapKey(ImGuiIOPtr io, ImGuiKey imKey, Keys xnaKey, KeyboardState state)
    {
        io.AddKeyEvent(imKey, state.IsKeyDown(xnaKey));
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
            return;

        UpdateBuffers(drawData);

        var viewport = _graphicsDevice.Viewport;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(
            0f,
            viewport.Width,
            viewport.Height,
            0f,
            0f,
            1f
        );

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;
        _graphicsDevice.RasterizerState = _rasterizerState;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                var cmd = cmdList.CmdBuffer[i];
                if (cmd.UserCallback != IntPtr.Zero)
                    continue;

                int sx = (int)Math.Max(cmd.ClipRect.X, 0);
                int sy = (int)Math.Max(cmd.ClipRect.Y, 0);
                int sw = (int)Math.Min(cmd.ClipRect.Z - cmd.ClipRect.X, viewport.Width - sx);
                int sh = (int)Math.Min(cmd.ClipRect.W - cmd.ClipRect.Y, viewport.Height - sy);

                if (sw <= 0 || sh <= 0)
                    continue;

                _graphicsDevice.ScissorRectangle = new Rectangle(sx, sy, sw, sh);
                _effect.Texture = _fontTexture;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        vtxOffset,
                        idxOffset + (int)cmd.IdxOffset,
                        (int)cmd.ElemCount / 3
                    );
                }
            }
            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    private void UpdateBuffers(ImDrawDataPtr drawData)
    {
        int vtxSize = Marshal.SizeOf<ImGuiVertex>();
        if (_vertexBuffer == null || drawData.TotalVtxCount > _vertexBufferSize)
        {
            _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
            _vertexBuffer?.Dispose();
            _vertexBuffer = new DynamicVertexBuffer(
                _graphicsDevice,
                typeof(ImGuiVertex),
                _vertexBufferSize,
                BufferUsage.WriteOnly
            );
        }

        if (_indexBuffer == null || drawData.TotalIdxCount > _indexBufferSize)
        {
            _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
            _indexBuffer?.Dispose();
            _indexBuffer = new DynamicIndexBuffer(
                _graphicsDevice,
                IndexElementSize.SixteenBits,
                _indexBufferSize,
                BufferUsage.WriteOnly
            );
        }

        int vtxByteOffset = 0;
        int idxByteOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            int vtxListByteSize = cmdList.VtxBuffer.Size * vtxSize;
            int idxListByteSize = cmdList.IdxBuffer.Size * sizeof(ushort);

            if (_vertexData.Length < vtxByteOffset + vtxListByteSize)
                Array.Resize(ref _vertexData, (vtxByteOffset + vtxListByteSize) * 2);
            if (_indexData.Length < idxByteOffset + idxListByteSize)
                Array.Resize(ref _indexData, (idxByteOffset + idxListByteSize) * 2);

            Marshal.Copy(cmdList.VtxBuffer.Data, _vertexData, vtxByteOffset, vtxListByteSize);
            Marshal.Copy(cmdList.IdxBuffer.Data, _indexData, idxByteOffset, idxListByteSize);

            vtxByteOffset += vtxListByteSize;
            idxByteOffset += idxListByteSize;
        }

        _vertexBuffer.SetData(_vertexData, 0, vtxByteOffset);
        _indexBuffer.SetData(_indexData, 0, idxByteOffset);
    }
}
