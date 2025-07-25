﻿using Data;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;

namespace Logic
{
    internal class Simulation : LogicAbstractAPI
    {
        private DataAbstractAPI board;
        private bool running { get; set; }
        private Thread collisionThread;
        private IBall[] balls;
        private ObservableCollection<IBall> observableData = new ObservableCollection<IBall>();
        public readonly object lockk = new object();
        
        public Simulation(DataAbstractAPI board = null)
        {
            if (board == null)
            {
                this.board = DataAbstractAPI.CreateDataAPI();
            }
            else
            {
                this.board = board;
            }
            this.running = false;
        }

        public void setBoard(IBoard board)
        {
            this.board = (DataAbstractAPI)board;
        }

        internal override DataAbstractAPI getBoard()
        {
            return board;
        }

        public override IBall[] getBalls()
        {
            return getBoard().getBalls();
        }

        public override void startSimulation()
        {
            string logFileName = $"Logger_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
            if (!running)
            {
                this.running = true;
                mainLoop();
            }
        }

        public override void stopSimulation()
        {
            if (running)
            {
                this.running = false;
                this.collisionThread.Interrupt();
                foreach (IBall b in balls)
                {
                    b.destroy();
                }
                DataLogger.GetInstance().stopRunning(); 
            }
        }

        private void mainLoop()
        {
            collisionThread = new Thread(() =>
            {
                try
                {
                    while (running)
                    {
                        lookForCollisions();
                        Thread.Sleep(5);
                    }
                }
                catch (ThreadInterruptedException) 
                {
                    Debug.WriteLine("Thread killed");
                }
            });
            collisionThread.IsBackground = true;
            collisionThread.Start();
        }

        private void lookForCollisions()
        {
            foreach (IBall ball1 in balls)
            {
                lock(lockk)
                {
                    checkBorderCollisionForBall(ball1);
                    foreach (IBall ball2 in balls)
                    {
                        if (ball1 == ball2)
                        { continue; }
                        Vector2 tmp1 = ball1.Pos;
                        Vector2 tmp2 = ball2.Pos;
                        if (Math.Sqrt((tmp1.X - tmp2.X) * (tmp1.X - tmp2.X) + (tmp1.Y - tmp2.Y) * (tmp1.Y - tmp2.Y)) <= ball1.getSize() / 2 + ball2.getSize() / 2)
                        {
                            ballCollision(ball1, ball2);
                        }
                    }
                }
            }
        }

        private void ballCollision(IBall ball1, IBall ball2)
        {
            DataLogger.GetInstance().LogCollision(
        new CollisionRecord(
            ball1.ID,
            ball2.ID,
            "Ball",
            ball1.Pos,
            DateTime.Now
        )
    );
            // Oblicz wektor normalny
            float dx = ball2.Pos.X - ball1.Pos.X;
            float dy = ball2.Pos.Y - ball1.Pos.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy); // odległość między kulami
            float n_x = dx / distance; // składowa x wektora normalnego
            float n_y = dy / distance; // składowa y wektora normalnego

            // Oblicz wektor styczny
            float t_x = -n_y; // składowa x wektora stycznego
            float t_y = n_x;  // składowa y wektora stycznego

            // Prędkości wzdłuż normalnej i stycznej
            float v1n = ball1.vel.X * (n_x) + ball1.vel.Y * (n_y);
            float v1t = ball1.vel.X * (t_x) + ball1.vel.Y * (t_y);

            float v2n = ball2.vel.X * (n_x) + ball2.vel.Y * (n_y);
            float v2t = ball2.vel.X * (t_x) + ball2.vel.Y * (t_y);

            // Nowe prędkości wzdłuż normalnej po zderzeniu
            float u1n = ((ball1.getMass() - ball2.getMass()) * v1n + 2 * ball2.getMass() * v2n) / (ball1.getMass() + ball2.getMass());
            float u2n = ((ball2.getMass() - ball1.getMass()) * v2n + 2 * ball1.getMass() * v1n) / (ball2.getMass() + ball1.getMass());

            // Nowe prędkości całkowite dla każdej kuli
            Vector2 vel1 = new Vector2(u1n * n_x + v1t * t_x, u1n * n_y + v1t * t_y);
            Vector2 vel2 = new Vector2(u2n * n_x + v2t * t_x, u2n * n_y + v2t * t_y);
            lock (lockk)
            {
                ball1.vel = vel1;
                ball2.vel = vel2;
            }
        }

        public void checkBorderCollisionForBall(IBall ball)
        {
            lock (lockk)
            {
                if (ball.Pos.X + ball.getSize() >= board.sizeX || ball.Pos.X + ball.vel.X + ball.getSize() >= board.sizeX)
                {
                    if (ball.vel.X > 0)
                    {
                        DataLogger.GetInstance().LogCollision(
                            new CollisionRecord(
                                ball.ID,
                                null,
                                "Wall",
                                ball.Pos,
                                DateTime.Now
                            )
                        );
                        Logic.changeXdirection(ball);
                    }
                }
                else if (ball.Pos.X <= 0 || ball.Pos.X + ball.vel.X <= 0)
                {
                    if (ball.vel.X < 0)
                    {
                        DataLogger.GetInstance().LogCollision(
                            new CollisionRecord(
                                ball.ID,
                                null,
                                "Wall",
                                ball.Pos,
                                DateTime.Now
                            )
                        );
                        Logic.changeXdirection(ball);
                    }
                }
                if (ball.Pos.Y + ball.getSize() >= board.sizeY || ball.Pos.Y + ball.vel.Y + ball.getSize() >= board.sizeY)
                {
                    if (ball.vel.Y > 0)
                    {
                        DataLogger.GetInstance().LogCollision(
                            new CollisionRecord(
                                ball.ID,
                                null,
                                "Wall",
                                ball.Pos,
                                DateTime.Now
                            )
                        );
                        Logic.changeYdirection(ball);
                    }
                }
                else if (ball.Pos.Y <= 0 || ball.Pos.Y + ball.vel.Y <= 0)
                {
                    if (ball.vel.Y < 0)
                    {
                        DataLogger.GetInstance().LogCollision(
                            new CollisionRecord(
                                ball.ID,
                                null,
                                "Wall",
                                ball.Pos,
                                DateTime.Now
                            )
                        );
                        Logic.changeYdirection(ball);
                    }
                }
            }
        }


        public override Vector2[] getCoordinates()
        {
            return board.getCoordinates();
        }

        #nullable enable
        public override event EventHandler<LogicEventArgs>? ChangedPosition;

        private void OnPropertyChanged(LogicEventArgs args)
        {
            ChangedPosition?.Invoke(this, args);
        }
        public override void getBoardParameters(int x, int y, int ballsAmount)
        {
            string logFileName = $"Logger_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
            DataLogger.ResetInstance(logFileName); // przed tworzeniem kul
            board.setBoardParameters(x, y, ballsAmount);
            foreach (IBall ball in board.getBalls())
            {
                this.observableData.Add(ball);
                ball.ChangedPosition += sendUpdate;
            }
            this.balls = board.getBalls();
        }


        public override void setBalls(IBall[] balls)
        {
            this.board.setBalls(balls);
        }

        private void sendUpdate(object sender, DataEventArgs e)
        {
            IBall ball  = (IBall)sender;
            Vector2 pos = ball.Pos;
            LogicEventArgs args = new LogicEventArgs(pos);
            OnPropertyChanged(args);
        }
    }
}