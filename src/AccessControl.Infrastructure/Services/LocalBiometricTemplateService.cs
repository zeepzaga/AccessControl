using FaceAiSharp;
using FaceAiSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AccessControl.Infrastructure.Services;

public class LocalBiometricTemplateService
{
    private readonly IFaceDetectorWithLandmarks _detector;
    private readonly IFaceEmbeddingsGenerator _embeddingsGenerator;

    public LocalBiometricTemplateService()
    {
        _detector = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();
        _embeddingsGenerator = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();
    }

    public float[]? CreateEmbedding(byte[] imageBytes)
    {
        using var image = Image.Load<Rgb24>(imageBytes);
        var faces = _detector.DetectFaces(image);
        if (faces.Count == 0)
        {
            return null;
        }

        var face = faces
            .OrderByDescending(x => x.Confidence)
            .First();

        if (face.Landmarks is null || face.Landmarks.Count < 5)
        {
            return null;
        }

        using var alignedFace = image.Clone();
        _embeddingsGenerator.AlignFaceUsingLandmarks(alignedFace, face.Landmarks);
        return _embeddingsGenerator.GenerateEmbedding(alignedFace);
    }

    public float Compare(float[] left, float[] right)
    {
        return left.Dot(right);
    }
}
