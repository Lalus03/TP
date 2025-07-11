﻿using Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Model
{
    public abstract class ModelAbstractAPI : IObservable<IBall>
    {
        public static ModelAbstractAPI CreateModelAPI()
        {
            return new Model();
        }
        public abstract void StartSimulation();
        public abstract void StopSimulation();
        public abstract IDisposable Subscribe(IObserver<IBall> observer);
        public abstract IBall[] getballs();
        #nullable enable
        public abstract event EventHandler<ModelEventArgs>? ChangedPosition;
        public abstract void getBoardParameters(int x, int y, int ballsAmount);
    }


    internal class Model : ModelAbstractAPI
    {
        private IObservable<EventPattern<BallChangeEventArgs>> eventObservable = null;
        public LogicAbstractAPI simulation { get; set; }
        #nullable enable
        public override event EventHandler<ModelEventArgs>? ChangedPosition;
        public event EventHandler<BallChangeEventArgs> BallChanged;
        public IObservable<EventHandler> ballsChanged;
        DrawBalls[] drawBalls;
        public override void getBoardParameters(int x, int y, int ballsAmount)
        {
            simulation.getBoardParameters(x, y, ballsAmount);
            drawBalls = new DrawBalls[ballsAmount];
            Vector2[] poss = simulation.getCoordinates();
            for (int i = 0; i < ballsAmount; i++)
            {
                DrawBalls ball = new DrawBalls(poss[i]);
                drawBalls[i] = ball;
                simulation.ChangedPosition += OnBallChanged;
            }
        }

        public override IBall[] getballs()
        {
            return drawBalls;
        }

        public Model(LogicAbstractAPI api = null)
        {
            if (api == null)
            {
                this.simulation = LogicAbstractAPI.CreateLogicAPI();
            }
            else
            {
                this.simulation = api;
            }
            eventObservable = Observable.FromEventPattern<BallChangeEventArgs>(this, "BallChanged");
        }

        public void setLogicAPI(LogicAbstractAPI api)
        {
            simulation = api;
        }

        private void OnBallChanged(object sender, LogicEventArgs e)
        {
            LogicAbstractAPI api = (LogicAbstractAPI)sender;
            Data.IBall[] balls = api.getBalls();
            foreach (Data.IBall ball in balls)
            {
                UpdatePosition();
            }
        }
        private void UpdatePosition()
        {
            var coords = simulation.getCoordinates();
            if (drawBalls == null || coords == null)
                return;

            int count = Math.Min(drawBalls.Length, coords.Length);
            for (int i = 0; i < count; i++)
            {
                if (drawBalls[i] != null)
                {
                    drawBalls[i].x = coords[i].X;
                    drawBalls[i].y = coords[i].Y;
                }
            }
        }
        public override void StartSimulation()
        {
            //button
            simulation.startSimulation();
        }

        public override void StopSimulation()
        {
            //button
            simulation.stopSimulation();
        }
        
        //source: https://github.com/mpostol/TP
        //presentation model sublayers are heavily inspired by that source, but implemented to fit our code
        public override IDisposable Subscribe(IObserver<IBall> observer)
        {
            return eventObservable.Subscribe(x => observer.OnNext(x.EventArgs.Ball), ex => observer.OnError(ex), observer.OnCompleted);
        }
    }
}
