using Chess960Game.Domain.Board;
using Chess960Game.Domain.Pieces;

namespace Chess960Game.Desktop.Animation;

public class MoveAnimation
{
    public bool IsActive { get; private set; }
    public Piece? Piece { get; private set; }
    public Position From { get; private set; }
    public Position To { get; private set; }
    public float Progress { get; private set; }
    public MoveAnimationType Type { get; private set; } = MoveAnimationType.Normal;

    public float ArcHeight { get; private set; }
    public float ScaleBoost { get; private set; }

    private const float NormalDuration = 0.28f;
    private const float AttackDuration = 0.65f;

    public void Start(Piece piece, Position from, Position to, MoveAnimationType type)
    {
        IsActive = true;
        Piece = piece;
        From = from;
        To = to;
        Progress = 0f;
        Type = type;

        if (type == MoveAnimationType.Attack)
        {
            ArcHeight = 180;
            ScaleBoost = 0.45f;
        }
        else
        {
            ArcHeight = 65;
            ScaleBoost = 0.05f;
        }
    }

    public bool Update(float deltaSeconds)
    {
        if (!IsActive)
            return false;

        float duration = Type == MoveAnimationType.Attack
            ? AttackDuration
            : NormalDuration;

        Progress += deltaSeconds / duration;

        if (Progress < 1f)
            return false;

        Progress = 1f;
        IsActive = false;

        bool finishedAttack = Type == MoveAnimationType.Attack;

        Piece = null;

        return finishedAttack;
    }
}