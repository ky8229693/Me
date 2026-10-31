namespace WEB_Demo.Api.Services;

public static class VectorMath
{
    public static void ValidateCollection(IReadOnlyList<float[]> vectors)
    {
        if (vectors.Count == 0) return;
        var dimension = vectors[0].Length;
        if (dimension == 0 || vectors.Any(vector => vector.Length != dimension || !IsValid(vector)))
            throw new EmbeddingUnavailableException("The embedding service returned invalid vectors.");
    }

    public static double CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || left.Length != right.Length || !IsValid(left) || !IsValid(right))
            throw new EmbeddingUnavailableException("Embedding vectors are incompatible.");
        double dot = 0, leftMagnitude = 0, rightMagnitude = 0;
        for (var index = 0; index < left.Length; index++)
        {
            dot += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }
        if (leftMagnitude == 0 || rightMagnitude == 0) throw new EmbeddingUnavailableException("Embedding vectors cannot be zero.");
        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private static bool IsValid(float[] vector) => vector.Any(value => value != 0) && vector.All(float.IsFinite);
}
