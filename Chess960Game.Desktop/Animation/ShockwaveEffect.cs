using Chess960Game.Domain.Board;
using Microsoft.Xna.Framework;
using System;

namespace Chess960Game.Desktop.Animation;

public class ShockwaveEffect
{
    private const float Duration = 0.7f;

    public bool IsActive { get; private set; }
    public Position Center { get; private set; }
    public float Timer { get; private set; }

    public void Start(Position center)
    {
        IsActive = true;
        Center = center;
        Timer = 0f;
    }

    public void Update(float deltaSeconds)
    {
        if (!IsActive)
            return;

        Timer += deltaSeconds;

        if (Timer >= Duration)
        {
            IsActive = false;
            Timer = 0f;
        }
    }

    public int GetOffset(
        Position position,
        Func<int, int> toScreenRow,
        Func<int, int> toScreenCol)
    {
        if (!IsActive)
            return 0;

        int screenRow = toScreenRow(position.Row);
        int screenCol = toScreenCol(position.Col);

        int centerRow = toScreenRow(Center.Row);
        int centerCol = toScreenCol(Center.Col);

        float distance = MathF.Sqrt(
            MathF.Pow(screenRow - centerRow, 2) +
            MathF.Pow(screenCol - centerCol, 2)
        );

        float time = Timer / Duration;

        float waveRadius = time * 6f;
        float waveWidth = 0.9f;

        float delta = MathF.Abs(distance - waveRadius);

        if (delta > waveWidth)
            return 0;

        float strength = 1f - delta / waveWidth;
        float fade = 1f - time;

        return (int)(-MathF.Sin(strength * MathF.PI) * 16f * fade);
    }
}