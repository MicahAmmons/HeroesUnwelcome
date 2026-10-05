using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NumVector2 = System.Numerics.Vector2;

namespace HeroesUnwelcomed.Game.DevTools;

/// <summary>
/// Connects Dear ImGui (the developer-panel library) to MonoGame.
/// </summary>
/// <remarks>
/// <para>
/// ImGui is "immediate mode": every frame you call functions like
/// <c>ImGui.Button("Restart")</c>, and the call both draws the button and
/// returns true if it was clicked. No button objects to create or keep in sync,
/// which is why it's popular for debug tools.
/// </para>
/// <para>
/// ImGui doesn't know how to draw anything itself. Each frame it hands back a
/// list of triangles (with colours and texture coordinates) plus "clip
/// rectangles". This class:
/// <list type="number">
///   <item>feeds ImGui the mouse and keyboard state (<see cref="BeforeLayout"/>),</item>
///   <item>uploads its font image to the GPU once,</item>
///   <item>copies its triangles into GPU buffers and draws them (<see cref="AfterLayout"/>).</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ImGuiRenderer : IDisposable
{
    // ImGui's vertex: position (2 floats), texture coordinate (2 floats), colour (4 bytes) = 20 bytes.
    private const int VertexSize = 20;
    private static readonly VertexDeclaration VertexLayout = new(
        VertexSize,
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0));

    private readonly Microsoft.Xna.Framework.Game _game;
    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly RasterizerState _rasterizer;
    private readonly Dictionary<IntPtr, Texture2D> _textures = [];
    private readonly Keys[] _allKeys = Enum.GetValues<Keys>();

    private DynamicVertexBuffer? _vertexBuffer;
    private DynamicIndexBuffer? _indexBuffer;
    private byte[] _vertexData = [];
    private byte[] _indexData = [];
    private int _vertexCapacity;
    private int _indexCapacity;
    private int _nextTextureId = 1;
    private int _lastScrollWheel;
    private int _lastHorizontalScrollWheel;

    public ImGuiRenderer(Microsoft.Xna.Framework.Game game, float uiScale)
    {
        _game = game;
        _device = game.GraphicsDevice;
        _effect = new BasicEffect(_device) { TextureEnabled = true, VertexColorEnabled = true };
        _rasterizer = new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true };

        ImGui.SetCurrentContext(ImGui.CreateContext());
        ImGuiIOPtr io = ImGui.GetIO();

        LoadFont(io, uiScale);
        ImGui.StyleColorsDark();
        ImGui.GetStyle().ScaleAllSizes(uiScale);

        // Typed characters (for text boxes) arrive through the window's text event.
        game.Window.TextInput += (_, e) =>
        {
            if (e.Character != '\t')
            {
                ImGui.GetIO().AddInputCharacter(e.Character);
            }
        };

        _lastScrollWheel = Mouse.GetState().ScrollWheelValue;
        _lastHorizontalScrollWheel = Mouse.GetState().HorizontalScrollWheelValue;
    }

    /// <summary>True when ImGui wants the mouse (pointer over a panel): the game should ignore it.</summary>
    public bool WantsMouse => ImGui.GetIO().WantCaptureMouse;

    /// <summary>True when ImGui wants the keyboard (typing in a field): the game should ignore it.</summary>
    public bool WantsKeyboard => ImGui.GetIO().WantCaptureKeyboard;

    /// <summary>Starts an ImGui frame. Call before any ImGui.* layout calls.</summary>
    public void BeforeLayout(GameTime gameTime)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DeltaTime = Math.Max((float)gameTime.ElapsedGameTime.TotalSeconds, 1e-5f);
        io.DisplaySize = new NumVector2(_device.PresentationParameters.BackBufferWidth, _device.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = NumVector2.One;

        if (_game.IsActive)
        {
            SendInput(io);
        }

        ImGui.NewFrame();
    }

    /// <summary>Finishes the ImGui frame and draws it on top of everything.</summary>
    public void AfterLayout()
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    private void LoadFont(ImGuiIOPtr io, float uiScale)
    {
        // ImGui's built-in font is a tiny pixel font that's hard to read on
        // high-DPI laptops. Prefer Windows' Segoe UI at a proper size.
        string segoe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
        if (File.Exists(segoe))
        {
            io.Fonts.AddFontFromFileTTF(segoe, 16f * uiScale);
        }
        else
        {
            io.Fonts.AddFontDefault();
            io.FontGlobalScale = uiScale;
        }

        // Turn ImGui's font atlas (every glyph packed into one image) into a texture.
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        byte[] data = new byte[width * height * bytesPerPixel];
        Marshal.Copy(pixels, data, 0, data.Length);

        var texture = new Texture2D(_device, width, height, false, SurfaceFormat.Color);
        texture.SetData(data);

        io.Fonts.SetTexID(BindTexture(texture));
        io.Fonts.ClearTexData(); // CPU copy no longer needed
    }

    // ImGui refers to textures by a number; we keep a lookup from number to Texture2D.
    private IntPtr BindTexture(Texture2D texture)
    {
        var id = new IntPtr(_nextTextureId++);
        _textures[id] = texture;
        return id;
    }

    private void SendInput(ImGuiIOPtr io)
    {
        MouseState mouse = Mouse.GetState();
        KeyboardState keyboard = Keyboard.GetState();

        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);

        int wheel = mouse.ScrollWheelValue - _lastScrollWheel;
        int horizontalWheel = mouse.HorizontalScrollWheelValue - _lastHorizontalScrollWheel;
        _lastScrollWheel = mouse.ScrollWheelValue;
        _lastHorizontalScrollWheel = mouse.HorizontalScrollWheelValue;
        io.AddMouseWheelEvent(horizontalWheel / 120f, wheel / 120f);

        // ImGui ignores repeated "still up"/"still down" events, so reporting every key every frame is fine.
        foreach (Keys key in _allKeys)
        {
            if (TryMapKey(key, out ImGuiKey imguiKey))
            {
                io.AddKeyEvent(imguiKey, keyboard.IsKeyDown(key));
            }
        }

        io.AddKeyEvent(ImGuiKey.ModCtrl, keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl));
        io.AddKeyEvent(ImGuiKey.ModShift, keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift));
        io.AddKeyEvent(ImGuiKey.ModAlt, keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt));
    }

    private static bool TryMapKey(Keys key, out ImGuiKey imguiKey)
    {
        // Letter, digit and function keys are laid out in the same order in both
        // enums, so ranges can be mapped with an offset.
        imguiKey = key switch
        {
            >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
            >= Keys.D0 and <= Keys.D9 => ImGuiKey._0 + (key - Keys.D0),
            >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
            >= Keys.F1 and <= Keys.F12 => ImGuiKey.F1 + (key - Keys.F1),
            Keys.Tab => ImGuiKey.Tab,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.Home => ImGuiKey.Home,
            Keys.End => ImGuiKey.End,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            Keys.Back => ImGuiKey.Backspace,
            Keys.Space => ImGuiKey.Space,
            Keys.Enter => ImGuiKey.Enter,
            Keys.Escape => ImGuiKey.Escape,
            Keys.LeftControl => ImGuiKey.LeftCtrl,
            Keys.RightControl => ImGuiKey.RightCtrl,
            Keys.LeftShift => ImGuiKey.LeftShift,
            Keys.RightShift => ImGuiKey.RightShift,
            Keys.LeftAlt => ImGuiKey.LeftAlt,
            Keys.RightAlt => ImGuiKey.RightAlt,
            _ => ImGuiKey.None,
        };
        return imguiKey != ImGuiKey.None;
    }

    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        // Remember the device state so the game's own drawing isn't affected.
        Viewport lastViewport = _device.Viewport;
        Rectangle lastScissor = _device.ScissorRectangle;
        RasterizerState lastRasterizer = _device.RasterizerState;
        BlendState lastBlend = _device.BlendState;
        DepthStencilState lastDepth = _device.DepthStencilState;
        SamplerState lastSampler = _device.SamplerStates[0];

        _device.BlendFactor = Color.White;
        _device.BlendState = BlendState.NonPremultiplied; // ImGui colours are not premultiplied
        _device.RasterizerState = _rasterizer;
        _device.DepthStencilState = DepthStencilState.None;
        _device.SamplerStates[0] = SamplerState.LinearClamp;
        _device.Viewport = new Viewport(0, 0, _device.PresentationParameters.BackBufferWidth, _device.PresentationParameters.BackBufferHeight);

        UploadBuffers(drawData);
        DrawCommands(drawData);

        _device.Viewport = lastViewport;
        _device.ScissorRectangle = lastScissor;
        _device.RasterizerState = lastRasterizer;
        _device.BlendState = lastBlend;
        _device.DepthStencilState = lastDepth;
        _device.SamplerStates[0] = lastSampler;
    }

    // Copies every panel's triangles into one big vertex buffer and one index buffer.
    private void UploadBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount > _vertexCapacity)
        {
            _vertexBuffer?.Dispose();
            _vertexCapacity = (int)(drawData.TotalVtxCount * 1.5f); // headroom, so we don't reallocate every frame
            _vertexBuffer = new DynamicVertexBuffer(_device, VertexLayout, _vertexCapacity, BufferUsage.None);
            _vertexData = new byte[_vertexCapacity * VertexSize];
        }

        if (drawData.TotalIdxCount > _indexCapacity)
        {
            _indexBuffer?.Dispose();
            _indexCapacity = (int)(drawData.TotalIdxCount * 1.5f);
            _indexBuffer = new DynamicIndexBuffer(_device, IndexElementSize.SixteenBits, _indexCapacity, BufferUsage.None);
            _indexData = new byte[_indexCapacity * sizeof(ushort)];
        }

        int vertexOffset = 0;
        int indexOffset = 0;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr list = drawData.CmdLists[n];
            Marshal.Copy(list.VtxBuffer.Data, _vertexData, vertexOffset * VertexSize, list.VtxBuffer.Size * VertexSize);
            Marshal.Copy(list.IdxBuffer.Data, _indexData, indexOffset * sizeof(ushort), list.IdxBuffer.Size * sizeof(ushort));
            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        _vertexBuffer!.SetData(_vertexData, 0, drawData.TotalVtxCount * VertexSize);
        _indexBuffer!.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
    }

    private void DrawCommands(ImDrawDataPtr drawData)
    {
        _device.SetVertexBuffer(_vertexBuffer);
        _device.Indices = _indexBuffer;

        // Map ImGui's pixel coordinates (0,0 top-left) straight onto the screen.
        _effect.World = Matrix.Identity;
        _effect.View = Matrix.Identity;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(0f, ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y, 0f, -1f, 1f);

        var screen = new Rectangle(0, 0, _device.PresentationParameters.BackBufferWidth, _device.PresentationParameters.BackBufferHeight);

        int vertexOffset = 0;
        int indexOffset = 0;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr list = drawData.CmdLists[n];

            for (int i = 0; i < list.CmdBuffer.Size; i++)
            {
                ImDrawCmdPtr command = list.CmdBuffer[i];
                if (command.ElemCount == 0)
                {
                    continue;
                }

                if (!_textures.TryGetValue(command.TextureId, out Texture2D? texture))
                {
                    throw new InvalidOperationException($"ImGui asked for unknown texture id {command.TextureId}.");
                }

                // The clip rectangle stops e.g. scrolled text from drawing outside its panel.
                var clip = new Rectangle(
                    (int)command.ClipRect.X,
                    (int)command.ClipRect.Y,
                    (int)(command.ClipRect.Z - command.ClipRect.X),
                    (int)(command.ClipRect.W - command.ClipRect.Y));
                clip = Rectangle.Intersect(clip, screen);
                if (clip.Width <= 0 || clip.Height <= 0)
                {
                    continue;
                }
                _device.ScissorRectangle = clip;

                _effect.Texture = texture;
                foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _device.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: (int)command.VtxOffset + vertexOffset,
                        startIndex: (int)command.IdxOffset + indexOffset,
                        primitiveCount: (int)command.ElemCount / 3);
                }
            }

            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _effect.Dispose();
        _rasterizer.Dispose();
        foreach (Texture2D texture in _textures.Values)
        {
            texture.Dispose();
        }
        ImGui.DestroyContext();
    }
}
