using System.Globalization;
using System.Text.Json;
using Laching.Companion.Figures;
using Laching.Companion.Rules;
using Laching.Testing.Conformance;
using Laching.Testing.Fixtures;
using Laching.Windows.Companion.Views;

namespace Laching.Windows.Companion.Tests.Views;

/// <summary>
/// What <see cref="CharacterCanvas"/> puts on screen, checked against <c>conformance/companion/characters</c>: each
/// part's XAML matrix maps its art to where the poses file says it goes, for the vector and the raster fixture.
/// </summary>
public sealed class CharacterSceneTests
{
    /// <summary>The compositor takes single-precision matrices: about seven digits, so 1e-4 at a 100-unit view box.</summary>
    private const double Tolerance = 1e-4;
    private static readonly string Root = ConformanceRoot.Find();
    private static readonly IReadOnlyList<RiggedCharacter> Characters = FixtureArt.Characters(Root);

    private static JsonElement Poses(string skin) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "companion", "characters", skin + "-poses.json"))).RootElement;

    private static RiggedCharacter Character(string skin) => Characters.Single(c => c.Skin.RawValue == skin);

    private static Mood MoodOf(JsonElement e) => Moods.Parse(e.GetProperty("mood").GetString()) ?? throw new InvalidDataException();

    [Fact]
    public void TheVectorPartsLandWhereThePosesFileDrawsTheirShapes()
    {
        var character = Character("fixture-vector");
        var cases = Poses("fixture-vector").GetProperty("shapes").EnumerateArray().ToArray();
        Assert.NotEmpty(cases);
        foreach (var c in cases)
        {
            var (mood, time) = (MoodOf(c), c.GetProperty("time").GetDouble());
            var drawn = character
                .Layers(character.Pose(mood, time))
                .SelectMany(layer =>
                    layer.Content is PartContent.Vector vector
                        ? vector.Shapes.Select(shape => shape.Path.SelectMany(command => command.Points).Select(p => CharacterScene.Apply(CharacterScene.Matrix(layer.Transform), p)).ToArray())
                        : []
                )
                .ToArray();
            var expected = c.GetProperty("shapes").EnumerateArray().Select(s => PointsOf(s.GetProperty("d").GetString()!)).ToArray();

            Assert.Equal(expected.Length, drawn.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Length, drawn[i].Length);
                for (var k = 0; k < expected[i].Length; k++)
                {
                    Assert.Equal(expected[i][k].X, drawn[i][k].X, Tolerance);
                    Assert.Equal(expected[i][k].Y, drawn[i][k].Y, Tolerance);
                }
            }
        }
    }

    [Fact]
    public void TheRasterPartsTakeThePosesFilesTransformsInItsOrder()
    {
        var character = Character("fixture-raster");
        var cases = Poses("fixture-raster").GetProperty("layers").EnumerateArray().ToArray();
        Assert.NotEmpty(cases);
        foreach (var c in cases)
        {
            var (mood, time) = (MoodOf(c), c.GetProperty("time").GetDouble());
            var layers = character.Layers(character.Pose(mood, time));
            var expected = c.GetProperty("layers").EnumerateArray().ToArray();

            Assert.Equal(expected.Length, layers.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].GetProperty("name").GetString(), layers[i].Name);
                var numbers = expected[i].GetProperty("transform").EnumerateArray().Select(n => n.GetDouble()).ToArray();
                var m = CharacterScene.Matrix(layers[i].Transform);
                double[] actual = [m.M11, m.M12, m.M21, m.M22, m.M41, m.M42];
                for (var k = 0; k < 6; k++)
                    Assert.Equal(numbers[k], actual[k], Tolerance);
            }
        }
    }

    [Fact]
    public void TheWakeSpringStartsAtZeroAndSettlesByItsDuration()
    {
        Assert.Equal(0, CharacterMotion.WakeProgress(0));
        Assert.Equal(1, CharacterMotion.WakeProgress(CharacterMotion.WakeDuration));
        Assert.Equal(1, CharacterMotion.WakeProgress(5));
        var half = CharacterMotion.WakeProgress(0.3);
        Assert.InRange(half, 0.5, 1);
        Assert.NotEqual(1, half);
        var last = -1.0;
        for (var step = 0; step <= 60; step++)
        {
            var progress = CharacterMotion.WakeProgress(step / 100.0);
            Assert.True(progress >= last);
            last = progress;
        }
    }

    [Fact]
    public void WakingBlendsFromTheShushedPose()
    {
        var character = Characters[0];
        var shushed = character.StillPose(Mood.Shushed);
        var awake = character.Pose(Mood.Awake, 2);

        Assert.Equal(shushed, CharacterMotion.Waking(shushed, awake, 0));
        Assert.Equal(awake, CharacterMotion.Waking(shushed, awake, 0.6));
        var early = CharacterMotion.Waking(shushed, awake, 0.05);
        Assert.InRange(early.HeadAngle, awake.HeadAngle, shushed.HeadAngle);
        Assert.NotEqual(awake.HeadAngle, early.HeadAngle);
        Assert.NotEqual(shushed.HeadAngle, early.HeadAngle);
    }

    [Fact]
    public void ThePoseHoldsStillTalksAndSpringsAsTheCanvasAsks()
    {
        var character = Characters[0];

        Assert.Equal(character.StillPose(Mood.Talking), CharacterMotion.PoseAt(character, Mood.Awake, talking: true, still: true, 3, 2, null));
        Assert.Equal(character.Pose(Mood.Talking, 3, 2), CharacterMotion.PoseAt(character, Mood.Awake, talking: true, still: false, 3, 2, null));
        // Not talking: the mouth level is ignored, the mood's own timer runs.
        Assert.Equal(character.Pose(Mood.Asleep, 3), CharacterMotion.PoseAt(character, Mood.Asleep, talking: false, still: false, 3, 2, null));
        // Just woken: the droop still shows; once the spring is over, it doesn't.
        var awake = character.Pose(Mood.Awake, 3);
        Assert.NotEqual(awake, CharacterMotion.PoseAt(character, Mood.Awake, talking: false, still: false, 3, null, 0.1));
        Assert.Equal(awake, CharacterMotion.PoseAt(character, Mood.Awake, talking: false, still: false, 3, null, CharacterMotion.WakeDuration));
    }

    [Fact]
    public void AnImagePointComesFromTheInverseOfThePartsTransform()
    {
        var frame = new Rect(10, 20, 40, 20);
        var transform = Transform.Rotation(90, new Point(0, 0)).Then(Transform.Translation(100, 0));

        // The frame's top-left corner, turned a quarter clockwise around the origin and moved right.
        var corner = transform.Apply(new Point(10, 20));
        var (u, v) = CharacterScene.ImagePoint(transform, frame, corner)!.Value;
        Assert.Equal(0, u, 1e-9);
        Assert.Equal(0, v, 1e-9);
        var middle = CharacterScene.ImagePoint(transform, frame, transform.Apply(new Point(30, 30)))!.Value;
        Assert.Equal((0.5, 0.5), (Math.Round(middle.U, 9), Math.Round(middle.V, 9)));
        Assert.Null(CharacterScene.ImagePoint(transform, frame, new Point(30, 30)));
        Assert.Null(CharacterScene.ImagePoint(Transform.Identity, frame, new Point(50, 30)));
    }

    [Fact]
    public void TheHitTestFindsFillsAndStrokesInThePoseDrawn()
    {
        var character = Character("fixture-vector");
        var awake = character.Layers(character.StillPose(Mood.Awake));
        var asleep = character.Layers(character.StillPose(Mood.Asleep));
        static AlphaMask? NoImages(string asset) => null;

        Assert.True(CharacterScene.IsDrawnAt(awake, new Point(50, 40), NoImages), "the face");
        Assert.True(CharacterScene.IsDrawnAt(awake, new Point(50, 85), NoImages), "the stem");
        Assert.False(CharacterScene.IsDrawnAt(awake, new Point(5, 5), NoImages), "a corner");
        Assert.False(CharacterScene.IsDrawnAt(awake, new Point(60, 80), NoImages), "beside the stem");
        // The sleeping "zz" is only strokes, 1.5 wide: on its lines, not between them.
        Assert.True(CharacterScene.IsDrawnAt(asleep, new Point(76, 10.5), NoImages));
        Assert.True(CharacterScene.IsDrawnAt(asleep, new Point(76, 14), NoImages));
        Assert.False(CharacterScene.IsDrawnAt(asleep, new Point(76, 12), NoImages));
        Assert.True(CharacterScene.IsDrawnAt(asleep, new Point(76, 12), NoImages, slop: 1.5));
        Assert.False(CharacterScene.IsDrawnAt(awake, new Point(76, 14), NoImages), "no zz awake");
    }

    [Fact]
    public void TheHitTestReadsAnImagesAlphaUnderItsTransform()
    {
        var character = Character("fixture-raster");
        var layers = character.Layers(character.StillPose(Mood.Awake));
        // The head drawn on its left half only.
        var alpha = Enumerable.Range(0, 100 * 100).Select(i => i % 100 < 50 ? (byte)255 : (byte)0).ToArray();
        var head = new AlphaMask(100, 100, alpha);
        AlphaMask? Masks(string asset) => asset == "fixture-raster.head" ? head : null;

        Assert.True(CharacterScene.IsDrawnAt(layers, new Point(30, 20), Masks));
        Assert.False(CharacterScene.IsDrawnAt(layers, new Point(70, 20), Masks));
        Assert.False(CharacterScene.IsDrawnAt(layers, new Point(5, 5), Masks));
    }

    [Theory]
    [InlineData("fixture-vector")]
    [InlineData("fixture-raster")]
    public void TheDrawnBoxesHoldEveryDrawnPointInEveryPose(string skin)
    {
        var character = Character(skin);
        // Every image fully drawn: the boxes must hold the whole frame.
        var solid = new AlphaMask(1, 1, [255]);
        AlphaMask? Masks(string asset) => solid;
        foreach (var mood in Enum.GetValues<Mood>())
            foreach (var time in new[] { 0, 0.37, 1.2, 2.9, 4.4 })
            {
                var layers = character.Layers(character.Pose(mood, time));
                var boxes = CharacterScene.DrawnBounds(layers);
                for (var x = 0.0; x <= 100; x += 1)
                    for (var y = 0.0; y <= 100; y += 1)
                        if (CharacterScene.IsDrawnAt(layers, new Point(x, y), Masks))
                            Assert.True(
                                boxes.Any(b => x >= b.Origin.X && x <= b.Origin.X + b.Size.Width && y >= b.Origin.Y && y <= b.Origin.Y + b.Size.Height),
                                $"{skin} {mood} at {time}: ({x}, {y}) drawn outside every box"
                            );
            }
    }

    [Fact]
    public void ATurnedPartsBoxHoldsItsTurnedCorners()
    {
        var head = Character("fixture-vector").Art.Parts["head"];
        var turned = new PlacedPart("head", Transform.Rotation(45, new Point(50, 40)), head);
        var box = Assert.Single(CharacterScene.DrawnBounds([turned]));
        // A 52-wide square turned 45° spans its diagonal, 52√2, plus the stroke turned with it.
        Assert.InRange(box.Size.Width, 52 * Math.Sqrt(2), 52 * Math.Sqrt(2) + 6);
        Assert.Equal(50, box.Origin.X + box.Size.Width / 2, 6);
    }

    [Fact]
    public void AShiftIsTheFarthestCornersMove()
    {
        var box = new Rect(0, 0, 30, 40);
        var turn = Transform.Rotation(1, new Point(0, 0));

        // The far corner (30, 40) is 50 from the pivot: a degree moves it 2 × 50 × sin 0.5°.
        Assert.Equal(100 * Math.Sin(Math.PI / 360), CharacterScene.Shift(Transform.Identity, turn, box), 1e-9);
        Assert.Equal(2, CharacterScene.Shift(Transform.Identity, Transform.Translation(0, 2), box), 1e-9);
        Assert.Equal(0, CharacterScene.Shift(turn, turn, box));
    }

    [Fact]
    public void APartsBoundsCoverItsPointsOrItsFrame()
    {
        var head = Character("fixture-vector").Art.Parts["head"];
        Assert.Equal(new Rect(24, 14, 52, 52), CharacterScene.Bounds(head));
        var raster = Character("fixture-raster").Art.Parts["head"];
        Assert.Equal(((PartContent.Image)raster).Frame, CharacterScene.Bounds(raster));
    }

    [Fact]
    public void TheViewBoxFitsTheSmallerSide() => Assert.Equal(2, CharacterScene.FitScale(new Size(100, 50), 200, 300));

    /// <summary>The points of a poses file's path, in order ("M x y C x1 y1 x2 y2 x y … Z").</summary>
    private static Point[] PointsOf(string d)
    {
        var numbers = d.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t is not ("M" or "L" or "C" or "Z"))
            .Select(t => double.Parse(t, CultureInfo.InvariantCulture))
            .ToArray();
        return Enumerable.Range(0, numbers.Length / 2).Select(i => new Point(numbers[2 * i], numbers[2 * i + 1])).ToArray();
    }
}
