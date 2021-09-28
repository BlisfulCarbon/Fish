using System;
using UnityEngine;

namespace Infrastructure.Interfaces.UI.Input
{
    public interface InputClick
    {
        public void SubscribeClick(Action action);
    }
}