using Raylib_cs;
using System.Numerics;

namespace PongRaylib
{
    internal class Pong
    {
        private static int _paddleWidth = 20;
        private static int _paddleHeight = 200;
        private static int _ballRadius = 10;
        private static Random _rand = new Random();

        public static float Paddle1Y = 400;
        public static int Paddle1Score = 0;
        public static int Paddle1Dir = 0;

        public static float Paddle2Y = 400;
        public static int Paddle2Score = 0;
        public static int Paddle2Dir = 0;

        public static Vector2 BallPosition = new Vector2(50, 400);
        public static Vector2 BallVelocity = new Vector2(0);

        public static User Serving = User.Player;
        public static User Target = User.Enemy;
        public static float EnemyAITimer = -1;
        public static int EnemyAIStartDir = 0;

        static void Main(string[] args)
        {
            Raylib.InitWindow(1280, 800, "Pong Raylib");

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                float frameTime = Raylib.GetFrameTime();

                MovePlayerPaddle(frameTime);
                MoveEnemyPaddle(frameTime);
                TickBall(frameTime);
                DrawBall();
                DrawPaddles();
                DrawScores();
                DrawSeperator();

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }

        public static void MovePlayerPaddle(float frameTime)
        {
            frameTime *= 500;

            Paddle1Dir = 0;
            if (Raylib.IsKeyDown(KeyboardKey.Up))
                Paddle1Dir = -1;
            if (Raylib.IsKeyDown(KeyboardKey.Down))
                Paddle1Dir = 1;

            Paddle1Y += frameTime * Paddle1Dir;
            Paddle1Y = Math.Clamp(Paddle1Y, 10 + _paddleHeight / 2, 790 - _paddleHeight / 2);
        }

        public static void MoveEnemyPaddle(float frameTime)
        {
            frameTime *= 500;
            float targetY = Target != User.Enemy ? 400 : CalcBallPath();

            if (EnemyAITimer == -1)
            {
                Paddle2Dir = 0;
                if (Paddle2Y > targetY)
                    Paddle2Dir = -1;
                if (Paddle2Y < targetY)
                    Paddle2Dir = 1;
            }
            else if (EnemyAITimer >= 0.25f)
            {
                if (EnemyAIStartDir == 0)
                    EnemyAIStartDir = _rand.Next(2) == 0 ? 1 : -1;
                Paddle2Dir = EnemyAIStartDir;
            }

            Paddle2Y += frameTime * Paddle2Dir;
            Paddle2Y = Math.Clamp(Paddle2Y, 10 + _paddleHeight / 2, 790 - _paddleHeight / 2);
        }

        public static void TickBall(float frameTime)
        {
            // Check Serve
            switch (Serving)
            {
                case User.Player:
                    if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    {
                        BallVelocity = new Vector2(0.2f, Paddle1Dir == -1 ? -0.2f : 0.2f);
                        Serving = User.None;
                    }
                    break;
                case User.Enemy:
                    if (_rand.Next(1000 - (int)(EnemyAITimer * 1000.0f)) == 0)
                    {
                        BallVelocity = new Vector2(-0.2f, Paddle2Dir == -1 ? -0.2f : 0.2f);
                        Serving = User.None;
                        EnemyAITimer = -1;
                        EnemyAIStartDir = 0;
                    }
                    break;
            }

            if (Serving != User.None)
                BallPosition.Y = Serving == User.Player ? Paddle1Y : Paddle2Y;

            // Check Collisions
            Rectangle ballRect = new Rectangle(BallPosition - new Vector2(_ballRadius), new Vector2(_ballRadius * 2 + 1));
            Rectangle paddle1Rect = new Rectangle(10, Paddle1Y - _paddleHeight / 2, _paddleWidth, _paddleHeight);
            Rectangle paddle2Rect = new Rectangle(1270 - _paddleWidth, Paddle2Y - _paddleHeight / 2, _paddleWidth, _paddleHeight);
            bool ballMovingDown = BallVelocity.Y > 0;
            switch (Target)
            {
                case User.Player:
                    if (Raylib.CheckCollisionRecs(ballRect, paddle1Rect) == true)
                    {
                        Target = User.Enemy;
                        BallVelocity.X *= -1;
                        BallVelocity.Y = 0.2f;
                        float dist = (float)Math.Sqrt(BallPosition.Y * BallPosition.Y + Paddle1Y * Paddle1Y) / 10000.0f;
                        if (ballMovingDown && Paddle1Dir == 1)
                            BallVelocity.Y += dist * 2;
                        else if (!ballMovingDown && Paddle1Dir == -1)
                            BallVelocity.Y += dist * 2;
                        else
                            BallVelocity.Y += dist;


                        BallVelocity.Y *= Paddle1Dir;
                    }
                    break;
                case User.Enemy:
                    if (Raylib.CheckCollisionRecs(ballRect, paddle2Rect) == true)
                    {
                        Target = User.Player;
                        BallVelocity.X *= -1;
                        BallVelocity.Y = 0.2f;
                        float dist = (float)Math.Sqrt(BallPosition.Y * BallPosition.Y + Paddle2Y * Paddle2Y) / 10000.0f;
                        if (ballMovingDown && Paddle2Dir == 1)
                            BallVelocity.Y += dist * 2;
                        else if (!ballMovingDown && Paddle2Dir == -1)
                            BallVelocity.Y += dist * 2;
                        else
                            BallVelocity.Y += dist;


                        BallVelocity.Y *= Paddle2Dir;
                    }
                    break;
            }
            if (BallPosition.Y - _ballRadius <= 0 || BallPosition.Y + _ballRadius >= 800)
                BallVelocity.Y *= -1;

            // Check Goals
            if (BallPosition.X + _ballRadius <= 0)
            {
                Serving = User.Player;
                Target = User.Enemy;
                Paddle2Score++;
                ResetBall();
            }
            else if (BallPosition.X - _ballRadius >= 1280)
            {
                Serving = User.Enemy;
                Target = User.Player;
                Paddle1Score++;
                ResetBall();
                EnemyAITimer = 0;
            }

            if (EnemyAITimer >= 0)
                EnemyAITimer += frameTime;

            // Move Ball
            BallPosition += BallVelocity;
        }

        public static void DrawBall()
        {
            Raylib.DrawRectangle((int)BallPosition.X - _ballRadius, (int)BallPosition.Y - _ballRadius, _ballRadius * 2 + 1, _ballRadius * 2 + 1, Color.White);
        }

        public static void DrawPaddles()
        {
            Raylib.DrawRectangle(10, (int)Paddle1Y - _paddleHeight / 2, _paddleWidth, _paddleHeight, Color.White);
            Raylib.DrawRectangle(1270 - _paddleWidth, (int)Paddle2Y - _paddleHeight / 2, _paddleWidth, _paddleHeight, Color.White);
        }

        public static void DrawScores()
        {
            Raylib.DrawText(Paddle1Score.ToString(),
                            1280 / 2 - Raylib.MeasureText(Paddle1Score.ToString(), 80) - 20,
                            20, 80, Color.White);

            Raylib.DrawText(Paddle2Score.ToString(),
                            1280 / 2 + 20,
                            20, 80, Color.White);
        }

        public static void DrawSeperator()
        {
            for (int i = 0; i < 800; i += 18)
                Raylib.DrawRectangle(1280 / 2 - 4, i, 9, 9, Color.Gray);
        }

        public static void ResetBall()
        {
            BallPosition = new Vector2(Serving == User.Player ? 30 + _paddleWidth : 1250 - _paddleWidth,
                                       Serving == User.Player ? Paddle1Y : Paddle2Y);
            BallVelocity = new Vector2(0);
        }

        public static int CalcBallPath()
        {
            Vector2 calcPos = BallPosition;
            if (BallVelocity.X == 0 || BallVelocity.Y == 0)
                return 400;
            while (true)
            {
                if (calcPos.Y - _ballRadius <= 0 || calcPos.Y + _ballRadius >= 800)
                    calcPos.Y *= -1;
                calcPos += BallVelocity;
                if (calcPos.X > 1250)
                    break;
            }
            return (int)calcPos.Y;
        }

        public enum User
        {
            None,
            Player,
            Enemy
        }
    }
}
