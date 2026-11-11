using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using BurnoutImage;
using PluginAPI;
using static BaseHandlers.LionEffect;

namespace BaseHandlers;

// Particle emitter simulation based on X360 Breaker Island build cParticleEmitter implementation
public sealed class LionEffectSimulation
{
    public sealed class Particle
    {
        public Vector3 Position, Velocity, Acceleration, Rotation, RotationVelocity, RotationAcceleration, Size, SizeVelocity, SizeAcceleration;
        public Vector4 Colour;
        public float Age, Life, TimeScale, Fps, Drag;
        public int Frame;
    }

    // Blends descriptor behaviours according to locator blend factor (cParticleEmitter::Blend)
    public sealed class Settings(Behaviour from, Behaviour to, float t)
    {
        private Record Near => t < 0.5f
            ? from.Fields
            : to.Fields;

        public float Float(string name) => float.Lerp(from.Fields.Float(name), to.Fields.Float(name), t);

        public Vector3 Vector(string name) => Vector3.Lerp(from.Fields.Vector(name), to.Fields.Vector(name), t);

        public Vector4 Colour(string name) => Vector4.Lerp(from.Fields.Colour(name), to.Fields.Colour(name), t);

        public bool Flag(string name) => Near.Flag(name);

        public int Number(string name) => Near.Number(name);
    }

    public sealed class Emitter(Descriptor descriptor)
    {
        public Descriptor Descriptor => descriptor;

        public string Name { get; } = descriptor.Fields.Text("NAME") ?? "Emitter";

        public bool Visible { get; set; } = true;

        public List<Particle> Particles { get; } = [];

        internal float Pause, Life, Repeat, Backlog;
        internal int Cycle = -1, Emitted, Limit;
        internal bool Started, Done;

        public Settings Settings(float blend) => descriptor.Behaviours.Count == 1
            ? new(descriptor.Behaviours[0], descriptor.Behaviours[0], 0)
            : new(descriptor.Behaviours[0], descriptor.Behaviours[1], Math.Clamp(blend, 0, 1));
    }

    private readonly int seed;
    private Random random;

    public List<Emitter> Emitters { get; }

    public float Time { get; private set; }

    // Locator blend factor between primary and secondary behaviours
    public float Blend { get; set; }

    public bool HasBlend => Emitters.Any(e => e.Descriptor.Behaviours.Count > 1);

    // True if all emitters have completed and no particles remain active
    public bool Finished => Emitters.All(e => e.Particles.Count == 0 && e.Descriptor.Fields.Number("EMITTER_LIFE_INFINITE") == 0
        && !e.Descriptor.Fields.Flag("DO_REPEAT") && Time > e.Pause + e.Life);

    public LionEffectSimulation(LionEffect effect, int initialSeed = 1)
    {
        seed = initialSeed;
        Emitters = [.. effect.Descriptors.Where(d => d.Behaviours.Count > 0 && !d.Fields.Flag("DISABLED_FLAG")).Select(d => new Emitter(d))];
        Restart();
    }

    public void Restart()
    {
        Time = 0;
        random = new(seed);

        foreach (Emitter emitter in Emitters)
        {
            emitter.Particles.Clear();
            emitter.Cycle = -1;
            emitter.Started = emitter.Done = false;
            emitter.Emitted = 0;
            emitter.Backlog = 0;
            Record d = emitter.Descriptor.Fields;
            emitter.Pause = d.Float("PAUSE_TIME") + Next() * d.Float("PAUSE_TIME_VARIANCE");
            emitter.Life = d.Float("EMITTER_LIFE_BASE") + Next() * d.Float("EMITTER_LIFE_VARIANCE");
        }
    }

    private float Next() => random.NextSingle();

    private Vector3 Next3() => new(Next(), Next(), Next());

    // CgsRandom integer range [low, low + range]
    private int Next(int low, int range) => low + random.Next(Math.Max(range, 0) + 1);

    public void Step(float dt)
    {
        Time += dt;

        foreach (Emitter emitter in Emitters)
        {
            Settings settings = emitter.Settings(Blend);

            if (Generating(emitter))
                Emit(emitter, settings, dt);
            else
            {
                emitter.Started = emitter.Done = false;
                emitter.Emitted = 0;
                emitter.Backlog = 0;
            }

            foreach (Particle particle in emitter.Particles)
                Advance(particle, settings, dt);

            emitter.Particles.RemoveAll(p => p.Age > p.Life);
        }
    }

    // cParticleEmitter::IsGenerating
    private bool Generating(Emitter emitter)
    {
        Record d = emitter.Descriptor.Fields;
        float local = Time - emitter.Pause;
        bool infinite = d.Number("EMITTER_LIFE_INFINITE") != 0;

        if (local < 0)
            return false;

        if (!d.Flag("DO_REPEAT"))
            return infinite || local <= emitter.Life;

        if (emitter.Repeat <= 0)
            emitter.Repeat = Math.Max(d.Float("REPEAT_TIME") + Next() * d.Float("REPEAT_TIME_VARIANCE"), 0.01f);

        int cycle = (int)(local / emitter.Repeat);

        if (cycle != emitter.Cycle)
        {
            emitter.Cycle = cycle;
            emitter.Repeat = Math.Max(d.Float("REPEAT_TIME") + Next() * d.Float("REPEAT_TIME_VARIANCE"), 0.01f);
            emitter.Started = emitter.Done = false;
            emitter.Emitted = 0;
        }

        return infinite || local - cycle * emitter.Repeat < emitter.Life;
    }

    // cParticleEmitter::Generate
    private void Emit(Emitter emitter, Settings settings, float dt)
    {
        if (emitter.Done)
            return;

        int clamp = settings.Number("EMISSION_COUNT_CLAMP"), clampVariance = settings.Number("EMISSION_CLAMP_VARIANCE");

        if (settings.Flag("DO_BURST"))
        {
            for (int i = Next(clamp, clampVariance) - emitter.Emitted; i > 0; i--)
                Spawn(emitter, settings, 0);

            emitter.Done = true;
            return;
        }

        float rate = Math.Max(settings.Float("EMISSION_RATE_BASE") + Next() * settings.Float("EMISSION_RATE_VARIANCE"), 0.1f);

        if (!emitter.Started)
        {
            emitter.Started = true;
            emitter.Limit = clamp != 0
                ? Next(clamp, clampVariance)
                : int.MaxValue;

            // DO_PREFORM: pre-warm emitter to simulate steady state
            if (emitter.Descriptor.Fields.Flag("DO_PREFORM"))
                emitter.Backlog = rate * (settings.Float("LIFE_BASE") + settings.Float("LIFE_VARIANCE"));
        }

        emitter.Backlog += rate * dt;

        for (int made = 0; emitter.Backlog >= 1 && emitter.Emitted < emitter.Limit && made < 256; made++)
        {
            emitter.Backlog -= 1;
            Spawn(emitter, settings, emitter.Backlog / rate);
        }

        if (emitter.Emitted >= emitter.Limit)
            emitter.Done = true;
    }

    // cParticleEmitter::InitialiseParticle
    private void Spawn(Emitter emitter, Settings s, float age)
    {
        Vector3 vary(string stem) => s.Vector(stem + "_BASE") + Next3() * s.Vector(stem + "_VARIANCE");

        Particle p = new()
        {
            Life = s.Float("LIFE_BASE") + Next() * s.Float("LIFE_VARIANCE"),
            TimeScale = s.Float("TIME_SCALE") + Next() * s.Float("TIME_SCALE_VARIANCE"),
            Position = vary("POS"),
            Velocity = vary("VEL"),
            Acceleration = vary("ACC")
        };

        if (s.Flag("DO_RADIAL"))
        {
            Vector3 direction = Next3() * 2 - Vector3.One;
            direction = direction == Vector3.Zero
                ? Vector3.UnitY
                : Vector3.Normalize(direction);
            p.Position += direction * s.Vector("RING_RADIUS");
            p.Velocity = direction * p.Velocity.Length();
        }

        // DO_ROTATE takes precedence over DO_ROTXYZ
        Vector3 axes = s.Flag("DO_ROTATE")
            ? Vector3.UnitZ
            : s.Flag("DO_ROTXYZ")
                ? Vector3.One
                : Vector3.Zero;
        p.Rotation = vary("ROTXYZ") * axes;
        p.RotationVelocity = vary("ROTXYZ_VEL") * axes;
        p.RotationAcceleration = vary("ROTXYZ_ACC") * axes;
        p.Size = vary("SIZEXYZ");
        p.SizeVelocity = vary("SIZEXYZ_VEL");
        p.SizeAcceleration = vary("SIZEXYZ_ACC");

        // BaseColourWithVarianceBehaviour
        Vector4 a = s.Colour("RGBA0"), b = s.Colour("RGBA1"), low = Vector4.Min(a, b), high = Vector4.Max(a, b);
        p.Colour = s.Number("RGBA_VARIANCE_MODE") switch
        {
            1 => low + new Vector4(Next(), Next(), Next(), Next()) * (high - low),
            2 => Vector4.Lerp(low, high, Next()),
            _ => a
        };

        Record material = emitter.Descriptor.Material;

        if (material != null && material.Flag("FLAG_MULTIFRAME"))
        {
            int frames = Math.Max(material.Number("XFRAMES") * material.Number("YFRAMES"), 1);
            p.Fps = material.Float("FPS") + Next() * material.Float("FPS_VARIANCE");
            p.Frame = material.Number("TEX_ANIM_OPTIONS") == 1
                ? random.Next(frames)
                : Next(material.Number("FRAME_BASE"), material.Number("FRAME_VARIANCE"));
        }

        emitter.Particles.Add(p);
        emitter.Emitted++;

        if (age > 0)
            Advance(p, s, age);
    }

    // Advances particle kinematics and updates rates
    private static void Advance(Particle p, Settings s, float dt)
    {
        p.Age += dt;
        float t = dt * p.TimeScale;
        p.Position += p.Velocity * (t * s.Float("SCALE"));
        p.Velocity += p.Acceleration * t;
        p.Rotation += p.RotationVelocity * t;
        p.RotationVelocity += p.RotationAcceleration * t;
        p.Size += p.SizeVelocity * t;
        p.SizeVelocity += p.SizeAcceleration * t;

        if (!s.Flag("DO_DRAG"))
            return;

        // DragBehaviour integration in 0.05s increments
        static Vector3 drag(Vector3 v, float k)
        {
            float length = v.Length();
            return length > 1e-7f
                ? v * (1 - Math.Min(1, k * length * length) / length)
                : v;
        }

        static float fraction(float value, float k) => value * (1 - Math.Min(1, k * Math.Abs(value)));

        float velocity = s.Float("DRAG_FACTOR_VEL"), rotation = s.Float("DRAG_FACTOR_ROT"), size = s.Float("DRAG_FACTOR_SCALE");

        for (p.Drag += t; p.Drag >= 0.05f; p.Drag -= 0.05f)
        {
            p.Velocity = drag(p.Velocity, velocity);

            if (s.Flag("DO_ROTATE"))
                p.RotationVelocity.Z = fraction(p.RotationVelocity.Z, rotation);
            else if (s.Flag("DO_ROTXYZ"))
                p.RotationVelocity = drag(p.RotationVelocity, rotation);

            if (s.Flag("DO_SIZEXYZ"))
                p.SizeVelocity = drag(p.SizeVelocity, size);
            else
                p.SizeVelocity.X = fraction(p.SizeVelocity.X, size);
        }
    }

    // SizeBehaviour scaling
    public static Vector3 SizeOf(Particle p, Settings s)
    {
        Vector3 shape = s.Vector("SIZEXYZ_BASE");
        Vector3 size = s.Flag("DO_SIZEXYZ")
            ? p.Size
            : s.Flag("DO_PROPORTIONAL") && shape.X != 0
                ? p.Size.X * shape / shape.X
                : new(p.Size.X);
        return size * s.Float("SCALE");
    }

    // Normalized particle lifetime [0, 1] (reversed if DO_REVERSE set)
    public static float LifeFraction(Particle p, Settings s)
    {
        float f = p.Life > 0
            ? Math.Clamp(p.Age / p.Life, 0, 1)
            : 1;

        return s.Flag("DO_REVERSE")
            ? 1 - f
            : f;
    }

    // Evaluates ColourStepsBehaviour and AlphaFadeBehaviour
    public static Vector4 ColourAt(Particle p, Settings s)
    {
        float f = LifeFraction(p, s);
        Vector4 colour = p.Colour;
        List<(float Time, Vector4 Colour)> steps = [];

        for (int i = 0; i < 4; i++)
            if (s.Flag($"DO_COLOURSTEP{i}"))
                steps.Add((s.Float($"COLOUR_TIME{i}"), s.Colour($"COLOUR{i}")));

        if (steps.Count > 0)
        {
            int next = steps.FindIndex(step => step.Time >= f);
            colour *= next switch
            {
                0 => steps[0].Colour,
                < 0 => steps[^1].Colour,
                _ => Vector4.Lerp(steps[next - 1].Colour, steps[next].Colour, steps[next].Time > steps[next - 1].Time
                    ? (f - steps[next - 1].Time) / (steps[next].Time - steps[next - 1].Time)
                    : 1)
            };
        }

        float fadeIn = s.Float("ALPHA_FADEIN"), fadeOut = s.Float("ALPHA_FADEOUT");

        if (fadeIn > f)
            colour.W *= f / fadeIn;
        else if (f > fadeOut && fadeOut < 1)
            colour.W *= 1 - (f - fadeOut) / (1 - fadeOut);

        // ConvertVector4ToRwRgbaOverbright: scale RGB by 511 and A by 255
        colour *= new Vector4(511f / 255, 511f / 255, 511f / 255, 1);
        return Vector4.Clamp(colour, Vector4.Zero, Vector4.One);
    }
}

// Software rasterizer for particle rendering with alpha blending
public sealed class LionEffectRenderer
{
    public sealed class Texture(int width, int height, Vector4[] pixels)
    {
        public int Width => width;

        public int Height => height;

        public Vector4[] Pixels => pixels;

        // Fallback radial texture when texture asset is unavailable
        public static readonly Texture Missing = Spot();

        private static Texture Spot()
        {
            Vector4[] pixels = new Vector4[32 * 32];

            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float d = Vector2.Distance(new(x + 0.5f, y + 0.5f), new(16)) / 16;
                    pixels[y * 32 + x] = new(1, 1, 1, Math.Clamp(1 - d, 0, 1));
                }

            return new(32, 32, pixels);
        }

        public static Texture FromImage(Image image)
        {
            using Bitmap bitmap = new(image);
            BitmapData bits = bitmap.LockBits(new(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] argb = new int[bitmap.Width * bitmap.Height];

            for (int y = 0; y < bitmap.Height; y++)
                Marshal.Copy(bits.Scan0 + y * bits.Stride, argb, y * bitmap.Width, bitmap.Width);

            bitmap.UnlockBits(bits);
            return new(bitmap.Width, bitmap.Height, [.. argb.Select(c => new Vector4((c >> 16) & 0xFF, (c >> 8) & 0xFF, c & 0xFF, (c >> 24) & 0xFF) / 255)]);
        }
    }

    private readonly Func<string, Texture> textures;
    private Vector3[] colour = [];
    private int width, height;

    public float Yaw { get; set; } = 0.7f;

    public float Pitch { get; set; } = 0.35f;

    public float Distance { get; set; } = 8;

    public Vector3 Target { get; set; } = new(0, 1, 0);

    public bool Grid { get; set; } = true;

    public Vector3 Background { get; set; } = new(0.08f);

    public LionEffectRenderer(Func<string, Texture> textureSource) => textures = textureSource;

    private Vector3 Eye => Target + Distance * new Vector3(MathF.Cos(Pitch) * MathF.Sin(Yaw), MathF.Sin(Pitch), MathF.Cos(Pitch) * MathF.Cos(Yaw));

    // Centers camera on primary particle density
    public void Frame(LionEffect effect)
    {
        LionEffectSimulation trial = new(effect);
        List<Vector3> positions = [];
        List<float> sizes = [];

        for (int i = 0; i < 90; i++)
        {
            trial.Step(1 / 30f);

            foreach (LionEffectSimulation.Emitter emitter in trial.Emitters)
                foreach (LionEffectSimulation.Particle p in emitter.Particles)
                {
                    Vector3 extent = LionEffectSimulation.SizeOf(p, emitter.Settings(0));
                    positions.Add(p.Position);
                    sizes.Add(Math.Max(extent.X, extent.Y));
                }
        }

        if (positions.Count == 0)
            return;

        Target = positions.Aggregate(Vector3.Add) / positions.Count;
        float[] reach = [.. positions.Select(p => Vector3.Distance(p, Target)).Order()];
        float[] size = [.. sizes.Order()];
        Distance = Math.Clamp(Math.Max(reach[(int)(reach.Length * 0.8f)], size[(int)(size.Length * 0.8f)] / 2) * 3, 0.5f, 100);
    }

    public void Render(LionEffectSimulation simulation, Bitmap target)
    {
        width = target.Width;
        height = target.Height;

        if (colour.Length != width * height)
            colour = new Vector3[width * height];

        Array.Fill(colour, Background);
        Matrix4x4 view = Matrix4x4.CreateLookAt(Eye, Target, Vector3.UnitY);
        Matrix4x4 viewProjection = view * Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3.5f, (float)width / height, 0.05f, 1000);
        Vector3 right = new(view.M11, view.M21, view.M31), up = new(view.M12, view.M22, view.M32), back = new(view.M13, view.M23, view.M33);

        if (Grid)
            for (int i = -5; i <= 5; i++)
            {
                float shade = i == 0
                    ? 0.35f
                    : 0.18f;
                Line(viewProjection, new(i, 0, -5), new(i, 0, 5), shade);
                Line(viewProjection, new(-5, 0, i), new(5, 0, i), shade);
            }

        // Back-to-front sorting for alpha blending
        List<(float Depth, LionEffectSimulation.Emitter Emitter, LionEffectSimulation.Particle Particle)> order = [];

        foreach (LionEffectSimulation.Emitter emitter in simulation.Emitters.Where(e => e.Visible))
            foreach (LionEffectSimulation.Particle particle in emitter.Particles)
                order.Add((Vector3.Distance(Eye, particle.Position), emitter, particle));

        foreach ((float _, LionEffectSimulation.Emitter emitter, LionEffectSimulation.Particle particle) in order.OrderByDescending(o => o.Depth))
            Quad(viewProjection, (right, up, back), emitter, emitter.Settings(simulation.Blend), particle);

        BitmapData bits = target.LockBits(new(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
        Parallel.For(0, height, y =>
        {
            int[] row = new int[width];

            for (int x = 0; x < width; x++)
            {
                Vector3 c = Vector3.Clamp(colour[y * width + x], Vector3.Zero, Vector3.One) * 255;
                row[x] = (int)c.X << 16 | (int)c.Y << 8 | (int)c.Z;
            }

            Marshal.Copy(row, 0, bits.Scan0 + y * bits.Stride, width);
        });

        target.UnlockBits(bits);
    }

    private Vector2 Screen(Vector4 clip) => new((clip.X / clip.W + 1) * 0.5f * width, (1 - clip.Y / clip.W) * 0.5f * height);

    private bool Project(Matrix4x4 viewProjection, Vector3 point, out Vector2 screen)
    {
        Vector4 clip = Vector4.Transform(new Vector4(point, 1), viewProjection);
        screen = Screen(clip);
        return clip.W > 0.05f && float.IsFinite(screen.X) && float.IsFinite(screen.Y);
    }

    // Near-plane line clipping
    private void Line(Matrix4x4 viewProjection, Vector3 from, Vector3 to, float shade)
    {
        Vector4 p = Vector4.Transform(new Vector4(from, 1), viewProjection), q = Vector4.Transform(new Vector4(to, 1), viewProjection);
        const float near = 0.05f;

        if (p.W < near && q.W < near)
            return;

        if (p.W < near)
            p = Vector4.Lerp(p, q, (near - p.W) / (q.W - p.W));
        else if (q.W < near)
            q = Vector4.Lerp(q, p, (near - q.W) / (p.W - q.W));

        // Liang-Barsky 2D viewport line clipping
        Vector2 a = Screen(p), d = Screen(q) - a;
        float t0 = 0, t1 = 1;

        foreach ((float towards, float room) in ((float, float)[])[(-d.X, a.X), (d.X, width - 1 - a.X), (-d.Y, a.Y), (d.Y, height - 1 - a.Y)])
            if (towards == 0)
            {
                if (room < 0)
                    return;
            }
            else if (towards < 0)
                t0 = Math.Max(t0, room / towards);
            else
                t1 = Math.Min(t1, room / towards);

        if (t0 > t1)
            return;

        int steps = (int)(Math.Max(Math.Abs(d.X), Math.Abs(d.Y)) * (t1 - t0));

        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = a + d * float.Lerp(t0, t1, steps == 0
                ? 0
                : (float)i / steps);
            int x = (int)point.X, y = (int)point.Y;

            if (x >= 0 && y >= 0 && x < width && y < height)
                colour[y * width + x] += new Vector3(shade);
        }
    }

    // LionParticleRender::Render quad vertex generation
    private void Quad(Matrix4x4 viewProjection, (Vector3 Right, Vector3 Up, Vector3 Back) camera, LionEffectSimulation.Emitter emitter, LionEffectSimulation.Settings settings, LionEffectSimulation.Particle particle)
    {
        Vector4 tint = LionEffectSimulation.ColourAt(particle, settings);
        Vector3 size = LionEffectSimulation.SizeOf(particle, settings), pivot = settings.Vector("PIVOT_POINT"), position = particle.Position;
        int shape = emitter.Descriptor.Fields.Number("SHAPE");
        Vector3[] corners;

        if (shape is 0 or 1)
        {
            // BuildBillboardCorners
            float x1 = size.X - size.X * pivot.X, y1 = size.Y - size.Y * pivot.Y, x0 = x1 - size.X, y0 = y1 - size.Y;
            Matrix4x4 turn = Matrix4x4.CreateRotationX(particle.Rotation.X) * Matrix4x4.CreateRotationY(particle.Rotation.Y) * Matrix4x4.CreateRotationZ(particle.Rotation.Z);
            corners = [.. ((Vector3[])[new(x0, y0, 0), new(x0, y1, 0), new(x1, y0, 0), new(x1, y1, 0)]).Select(c => Vector3.Transform(c, turn)).Select(c => shape == 0
                ? position + c.X * camera.Right + c.Y * camera.Up + c.Z * camera.Back
                : position + c)];
        }
        else if (shape is 3 or 4)
        {
            // Segment alignment for TILT (velocity-aligned) and AXIS (fixed-axis)
            Vector3 segment = shape == 4
                ? settings.Vector("AXIS_BASE")
                : particle.Velocity.LengthSquared() > 1.2e-7f
                    ? particle.Velocity * 0.05f
                    : Vector3.UnitY;
            Vector3 along = segment.LengthSquared() > 1e-12f
                ? Vector3.Normalize(segment)
                : Vector3.UnitY;
            Vector3 span = along * size.Y, end0 = position - span * pivot.Y, middle = end0 + span * 0.5f;
            bool endOn = settings.Flag("DO_ENDON_ACTIVE");
            float start = settings.Float("END_ON_START_ANGLE"), end = settings.Float("END_ON_END_ANGLE");

            // End-on orientation factor
            float facing()
            {
                float dot = Vector3.Dot(along, Vector3.Normalize(Eye - middle));
                dot *= dot;
                return dot <= start
                    ? 0
                    : dot >= end
                        ? 1
                        : (dot - start) / (end - start);
            }

            if (endOn && settings.Flag("DO_ENDON_SPRITE"))
            {
                // End-on billboard sprite
                float x0 = size.X * -pivot.X, y0 = size.Y * -pivot.Y, x1 = size.X + x0, y1 = size.Y + y0;
                float cos = MathF.Cos(particle.Rotation.Z), sin = MathF.Sin(particle.Rotation.Z);
                tint.W = facing();
                corners = [.. ((Vector2[])[new(x0, y0), new(x0, y1), new(x1, y0), new(x1, y1)]).Select(c => middle + (c.X * cos + c.Y * sin) * camera.Right + (c.Y * cos - c.X * sin) * camera.Up)];
            }
            else
            {
                // Segment ribbon with end-on minimum width and alpha clamp
                Vector3 width = Vector3.Cross(along, Vector3.Normalize(Eye - (position + segment * pivot.Y)));

                if (endOn)
                {
                    float length = width.Length();
                    width = (length > 0
                        ? width / length
                        : Vector3.Zero) * Math.Max(length, settings.Float("END_ON_SCALE"));
                    tint.W *= Math.Max(1 - facing(), settings.Float("END_ON_ALPHA_FADE"));
                }

                width *= size.X;
                Vector3 a = end0 - width * pivot.X, b = a + span;
                corners = [a, b, a + width, b + width];
            }
        }
        else
            return;

        if (tint.W <= 0.002f)
            return;

        Record material = emitter.Descriptor.Material;

        // UV coordinate calculation for sprite sheets
        Texture texture = textures(material?.Text("TEXTURE")) ?? Texture.Missing;
        int across = 1, down = 1, frame = 0;

        if (material != null && material.Flag("FLAG_MULTIFRAME"))
        {
            across = Math.Max(material.Number("XFRAMES"), 1);
            down = Math.Max(material.Number("YFRAMES"), 1);
            int frames = across * down;
            int played = (int)(particle.Age * particle.Fps);
            frame = material.Number("TEX_ANIM_OPTIONS") switch
            {
                2 => Math.Min(particle.Frame + played, frames - 1),
                3 => (int)(LionEffectSimulation.LifeFraction(particle, settings) * (frames - 1) + 0.5f),
                _ => (particle.Frame + played) % frames
            };
            frame = Math.Clamp(frame, 0, frames - 1);
        }

        Vector2 uv0 = new((float)(frame % across) / across, (float)(frame / across) / down), uv1 = uv0 + new Vector2(1f / across, 1f / down);
        string blend = material == null
            ? "SRCALPHA"
            : material.Value(Array.Find(MaterialTokens, t => t.Name == "BLEND_MODE"));
        int mode = blend.Contains("ADD")
            ? 1
            : blend.Contains("SUB") || blend.Contains("MINUS")
                ? 2
                : 0;

        Vector2[] uv = [new(uv0.X, uv1.Y), uv0, uv1, new(uv1.X, uv0.Y)], screen = new Vector2[4];

        for (int i = 0; i < 4; i++)
            if (!Project(viewProjection, corners[i], out screen[i]))
                return;

        Triangle(screen[0], screen[1], screen[3], uv[0], uv[1], uv[3], texture, tint, mode);
        Triangle(screen[0], screen[3], screen[2], uv[0], uv[3], uv[2], texture, tint, mode);
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

    // Parallel span-based software rasterizer
    private void Triangle(Vector2 a, Vector2 b, Vector2 c, Vector2 ta, Vector2 tb, Vector2 tc, Texture texture, Vector4 tint, int mode)
    {
        float area = Edge(a, b, c);

        if (MathF.Abs(area) < 1e-4f)
            return;

        int x0 = Math.Max((int)MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X))), 0), x1 = Math.Min((int)MathF.Ceiling(MathF.Max(a.X, MathF.Max(b.X, c.X))), width - 1);
        int y0 = Math.Max((int)MathF.Floor(MathF.Min(a.Y, MathF.Min(b.Y, c.Y))), 0), y1 = Math.Min((int)MathF.Ceiling(MathF.Max(a.Y, MathF.Max(b.Y, c.Y))), height - 1);

        float inverse = 1 / area, waX = (c.Y - b.Y) * inverse, wbX = (a.Y - c.Y) * inverse;
        Vector3[] pixels = colour;
        int stride = width;

        void row(int y)
        {
            Vector2 start = new(x0 + 0.5f, y + 0.5f);
            float wa = Edge(b, c, start) * inverse, wb = Edge(c, a, start) * inverse;

            for (int x = x0; x <= x1; x++, wa -= waX, wb -= wbX)
            {
                float wc = 1 - wa - wb;

                if (wa < 0 || wb < 0 || wc < 0)
                    continue;

                Vector4 texel = Sample(texture, ta * wa + tb * wb + tc * wc) * tint;
                Vector3 rgb = new Vector3(texel.X, texel.Y, texel.Z) * texel.W;
                ref Vector3 pixel = ref pixels[y * stride + x];
                pixel = mode switch
                {
                    1 => pixel + rgb,
                    2 => pixel - rgb,
                    _ => rgb + pixel * (1 - texel.W)
                };
            }
        }

        if ((long)(x1 - x0 + 1) * (y1 - y0 + 1) > 8192)
            Parallel.For(y0, y1 + 1, row);
        else
            for (int y = y0; y <= y1; y++)
                row(y);
    }

    private static Vector4 Sample(Texture texture, Vector2 uv)
    {
        float fx = Math.Clamp(uv.X * texture.Width - 0.5f, 0, texture.Width - 1), fy = Math.Clamp(uv.Y * texture.Height - 0.5f, 0, texture.Height - 1);
        int x = (int)fx, y = (int)fy, x2 = Math.Min(x + 1, texture.Width - 1), y2 = Math.Min(y + 1, texture.Height - 1);
        float dx = fx - x, dy = fy - y;
        Vector4[] p = texture.Pixels;
        return Vector4.Lerp(Vector4.Lerp(p[y * texture.Width + x], p[y * texture.Width + x2], dx), Vector4.Lerp(p[y2 * texture.Width + x], p[y2 * texture.Width + x2], dx), dy);
    }

    // Resolves effect textures via archive TextureNameMap
    public static Func<string, Texture> BundleTextures(BundleArchive archive)
    {
        Dictionary<string, BundleEntry> entries = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Texture> decoded = new(StringComparer.OrdinalIgnoreCase);

        if (archive?.Entries.FirstOrDefault(e => e.Type == EntryType.TextureNameMap) is BundleEntry mapEntry)
        {
            TextureNameMap map = new();
            map.Read(mapEntry);

            foreach (TextureNameMap.Entry name in map.mpEntries)
                if (archive.Entries.FirstOrDefault(e => e.ID == CRC32.HashEntryID(name.mpGDBTextureName)) is BundleEntry texture)
                    entries[name.LionName] = texture;
        }

        return name =>
        {
            if (name == null || !entries.TryGetValue(name, out BundleEntry entry))
                return null;

            if (!decoded.TryGetValue(name, out Texture texture))
            {
                try
                {
                    using Image image = GameImage.GetImage(entry.EntryBlocks[0].Data, entry.EntryBlocks[1].Data, entry.Platform);
                    texture = image == null
                        ? null
                        : Texture.FromImage(image);
                }
                catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or ArgumentException)
                {
                    texture = null;
                }

                decoded[name] = texture;
            }

            return texture;
        };
    }
}

// Particle effect playback viewer
public sealed class LionEffectViewer : Form, IEntryEditor
{
    private sealed class Canvas : Control
    {
        public Bitmap Frame;

        public Canvas() => SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Frame == null)
                return;

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.DrawImage(Frame, ClientRectangle);
        }
    }

    private readonly LionEffect effect;
    private readonly BundleEntry entry;
    private readonly LionEffectRenderer renderer;

    private readonly Canvas canvas = new()
    {
        Dock = DockStyle.Fill
    };

    private readonly CheckedListBox emitters = new()
    {
        Dock = DockStyle.Left,
        Width = 190,
        CheckOnClick = true,
        IntegralHeight = false
    };

    private readonly TrackBar blend = new()
    {
        Minimum = 0,
        Maximum = 100,
        TickStyle = TickStyle.None,
        AutoSize = false,
        Height = 24,
        Width = 140
    };

    private readonly List<ToolStripItem> blendItems = [];
    private LionEffectSimulation simulation;

    private readonly Timer timer = new()
    {
        Interval = 33
    };

    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly ToolStripLabel status = new();
    private float speed = 1, idle;
    private bool playing = true, looping = true;
    private Point dragFrom;
    private double last;

    public LionEffectSimulation Simulation => simulation;

    public LionEffectViewer(LionEffect source, BundleEntry initialEntry)
    {
        effect = source;
        entry = initialEntry;
        renderer = new(LionEffectRenderer.BundleTextures(entry.Archive));
        Text = $"{source.Name} — {entry.ID:X8}";
        ClientSize = new(1000, 680);
        StartPosition = FormStartPosition.CenterParent;
        emitters.ItemCheck += (_, e) => simulation.Emitters[e.Index].Visible = e.NewValue == CheckState.Checked;

        ToolStrip tools = new()
        {
            GripStyle = ToolStripGripStyle.Hidden
        };

        ToolStripButton play = new("Pause");
        play.Click += (_, _) =>
        {
            playing = !playing;
            play.Text = playing
                ? "Pause"
                : "Play";
        };

        ToolStripComboBox speeds = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            AutoSize = false,
            Width = 70
        };

        speeds.Items.AddRange(["0.25×", "0.5×", "1×", "2×"]);
        speeds.SelectedIndex = 2;
        speeds.SelectedIndexChanged += (_, _) => speed = (float)Math.Pow(2, speeds.SelectedIndex - 2);
        ToolStripButton grid = new("Grid")
        {
            Checked = true,
            CheckOnClick = true
        };

        grid.CheckedChanged += (_, _) => renderer.Grid = grid.Checked;
        ToolStripButton loop = new("Loop")
        {
            Checked = true,
            CheckOnClick = true,
            ToolTipText = "Play a one-shot effect again once it's over."
        };

        loop.CheckedChanged += (_, _) => looping = loop.Checked;
        ToolStripButton light = new("Light")
        {
            CheckOnClick = true,
            ToolTipText = "A light background, for dark smoke and dust."
        };

        light.CheckedChanged += (_, _) => renderer.Background = new(light.Checked
            ? 0.6f
            : 0.08f);
        tools.Items.AddRange([play, new ToolStripButton("Restart", null, (_, _) => simulation.Restart()), new ToolStripLabel("Speed"), speeds, loop, grid, light,
            new ToolStripButton("Frame", null, (_, _) => renderer.Frame(effect))]);

        blend.ValueChanged += (_, _) => simulation.Blend = blend.Value / 100f;
        blendItems.AddRange([new ToolStripLabel("Blend")
        {
            ToolTipText = "The game blends emitters with two behaviours by a value it sets, such as the boost level."
        }, new ToolStripControlHost(blend)]);
        tools.Items.AddRange([.. blendItems]);
        tools.Items.Add(new ToolStripButton("Export .lef…", null, (_, _) => Export()));
        tools.Items.Add(new ToolStripButton("Import .lef…", null, (_, _) => Import())
        {
            ToolTipText = "Replace this effect with a .lef saved by the LION editor."
        });
        tools.Items.Add(new ToolStripSeparator());
        tools.Items.Add(status);
        Reload();

        canvas.MouseDown += (_, e) => dragFrom = e.Location;
        canvas.MouseMove += (_, e) =>
        {
            if (e.Button == MouseButtons.None)
                return;

            if (e.Button == MouseButtons.Left)
            {
                renderer.Yaw -= (e.X - dragFrom.X) * 0.01f;
                renderer.Pitch = Math.Clamp(renderer.Pitch + (e.Y - dragFrom.Y) * 0.01f, -1.5f, 1.5f);
            }
            else
                renderer.Target += new Vector3(0, (e.Y - dragFrom.Y) * 0.01f * renderer.Distance / 5, 0);

            dragFrom = e.Location;
        };
        canvas.MouseWheel += (_, e) => renderer.Distance = Math.Clamp(renderer.Distance * MathF.Pow(0.9f, e.Delta / 120f), 0.5f, 200);
        canvas.Resize += (_, _) => Draw();

        Controls.Add(canvas);
        Controls.Add(emitters);
        Controls.Add(tools);
        timer.Tick += (_, _) => Tick();
        Shown += (_, _) => timer.Start();
        FormClosed += (_, _) =>
        {
            timer.Stop();
            canvas.Frame?.Dispose();
        };
    }

    // A new simulation of the effect, after opening or importing
    private void Reload()
    {
        simulation = new(effect)
        {
            Blend = blend.Value / 100f
        };

        emitters.Items.Clear();

        foreach (LionEffectSimulation.Emitter emitter in simulation.Emitters)
            emitters.Items.Add($"{emitter.Name} ({emitter.Descriptor.Material?.Text("TEXTURE") ?? "no material"})", true);

        foreach (ToolStripItem item in blendItems)
            item.Visible = simulation.HasBlend;

        renderer.Frame(effect);
    }

    private void Import()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Import LION effect",
            Filter = "LION effect (*.lef)|*.lef|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        List<string> notes = [];
        byte[] data;

        // Validates compilation on a copy before applying changes
        try
        {
            data = effect.Compile(File.ReadAllText(dialog.FileName), entry.Console, notes);
            BundleEntry trial = new(entry.Archive)
            {
                ID = entry.ID,
                Type = entry.Type,
                Platform = entry.Platform,
                EntryBlocks = [new()
                {
                    Data = data
                }]
            };

            LionEffect.FromEntry(trial);
        }
        catch (Exception ex) when (ex is InvalidDataException or ReadFailedError or IOException)
        {
            MessageBox.Show(this, ex.Message, "Can't import this effect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        entry.SetPrimary(data, entry.GetImports());
        effect.Read(entry);
        Reload();

        foreach (string texture in effect.Descriptors.Select(d => d.Material?.Text("TEXTURE")).Where(t => t != null && t != "(NULL)").Distinct())
            if (LionEffectRenderer.BundleTextures(entry.Archive)(texture) == null)
                notes.Add($"There's no {texture} in this bundle's effect textures, so the game won't find it.");

        MessageBox.Show(this, $"Imported {Path.GetFileName(dialog.FileName)}. Save the bundle to keep it.{(notes.Count > 0
            ? "\n\n" + string.Join("\n\n", notes)
            : "")}", "Import LION effect", MessageBoxButtons.OK, notes.Count > 0
            ? MessageBoxIcon.Warning
            : MessageBoxIcon.Information);
    }

    private void Tick()
    {
        double now = clock.Elapsed.TotalSeconds;
        float dt = (float)Math.Min(now - last, 0.1);
        last = now;

        if (playing)
        {
            simulation.Step(dt * speed);

            // Replay delay for one-shot effects
            idle = looping && simulation.Finished
                ? idle + dt * speed
                : 0;

            if (idle > 0.5f)
                simulation.Restart();
        }

        Draw();
    }

    private void Draw()
    {
        if (canvas.Width <= 0 || canvas.Height <= 0)
            return;

        // Renders at 640px maximum dimension and scales to display
        float scale = Math.Min(1, 640f / canvas.ClientSize.Width);
        Size size = new(Math.Max((int)(canvas.ClientSize.Width * scale), 1), Math.Max((int)(canvas.ClientSize.Height * scale), 1));

        if (canvas.Frame == null || canvas.Frame.Size != size)
        {
            canvas.Frame?.Dispose();
            canvas.Frame = new(size.Width, size.Height, PixelFormat.Format32bppRgb);
        }

        renderer.Render(simulation, canvas.Frame);
        status.Text = $"{simulation.Time:0.00} s, {simulation.Emitters.Sum(e => e.Particles.Count)} particles";
        canvas.Invalidate();
    }

    private void Export()
    {
        using SaveFileDialog dialog = new()
        {
            Title = "Export LION effect",
            FileName = effect.Name,
            Filter = "LION effect (*.lef)|*.lef|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            File.WriteAllText(dialog.FileName, effect.ToText());
    }
}
