using System;
using com.cozyhome.Systems;
using com.cozyhome.ConcurrentExecution;
using UnityEngine;

namespace com.cozyhome.Console
{


    [System.Serializable]
    public class ConsoleArgs
    {
        public Action<bool, ConsoleArgs> OnConsoleChanged;
        public Vector2[] DrawerPositions;
        public RectTransform Drawer;

        public int InputFlags = 0;
        public string InputString = "NIL";
    }

    [System.Serializable]
    public class ConsoleInput : ConcurrentHeader.ExecutionMachine<ConsoleArgs>.ConcurrentExecution
    {
        [SerializeField] KeyCode Toggle = KeyCode.BackQuote;
        [SerializeField] KeyCode Backspace = KeyCode.Backspace;

        protected override void OnExecutionDiscovery(ConsoleArgs Middleman)
        {
            RegisterExecution();
            BeginExecution();
        }
        public override void Simulate(ConsoleArgs _args)
        {
            int _flags = 0;

            _flags |= Input.GetKey(Toggle) ? 0x0001 : 0x0000;
            _flags |= Input.GetKey(Backspace) ? 0x0002 : 0x0000;

            _args.InputFlags = _flags;
            _args.InputString = Input.inputString;
        }
    }

    [System.Serializable]
    public class ConsoleActivity : ConcurrentHeader.ExecutionMachine<ConsoleArgs>.ConcurrentExecution
    {
        [Header("Activity Settings")]
        [SerializeField] private float ToggleDelay = 0.5F;
        [SerializeField] private float LastInputSnapshot = 0.0F;
        [SerializeField] private bool IsActive = false;
        protected override void OnExecutionDiscovery(ConsoleArgs Middleman)
        {
            RegisterExecution();
            BeginExecution();
        }
        public override void Simulate(ConsoleArgs _args)
        {
            if ((_args.InputFlags & 0x0001) != 0 &&
                GlobalTime.T - LastInputSnapshot > ToggleDelay)
            {
                IsActive = !IsActive;
                LastInputSnapshot = GlobalTime.T;

                _args.OnConsoleChanged?.Invoke(IsActive, _args);
            }
        }
    }

    [System.Serializable]
    public class ConsoleEnable : ConcurrentHeader.ExecutionMachine<ConsoleArgs>.ConcurrentExecution
    {

        [System.NonSerialized] float InterpTime = 0F;
        protected override void OnExecutionDiscovery(ConsoleArgs _args)
        {
            RegisterExecution();

            _args.OnConsoleChanged += OnConsoleToggled;
        }
        public override void Simulate(ConsoleArgs _args)
        {
            InterpTime += GlobalTime.DT;
            InterpTime = Mathf.Min(InterpTime, 1F);

            _args.Drawer.anchoredPosition = new Vector2(
                0F,
                _args.DrawerPositions[0][1] +
                InterpTime * (_args.DrawerPositions[1][1] - _args.DrawerPositions[0][1])
            );

            if (InterpTime >= 1)
            {
                EndExecution();
            }
        }

        public void OnConsoleToggled(bool B, ConsoleArgs _args)
        {
            if (B) // Active means closed to open
            {
                // distance along line
                float relx = Mathf.Abs(_args.Drawer.anchoredPosition[1] - _args.DrawerPositions[0][1]);

                // distance of entire line
                float maxx = Mathf.Abs(_args.DrawerPositions[1][1] - _args.DrawerPositions[0][1]);
                InterpTime = relx / maxx;

                BeginExecution();
            }
            else
                EndExecution();
        }
    }

    [System.Serializable]
    public class ConsoleDisable : ConcurrentHeader.ExecutionMachine<ConsoleArgs>.ConcurrentExecution
    {
        [System.NonSerialized] float InterpTime = 0F;
        protected override void OnExecutionDiscovery(ConsoleArgs _args)
        {
            _args.OnConsoleChanged += OnConsoleToggled;
        }
        public override void Simulate(ConsoleArgs _args)
        {
            InterpTime += GlobalTime.DT;
            InterpTime = Mathf.Min(InterpTime, 1F);

            _args.Drawer.anchoredPosition = new Vector2(
                0F,
                _args.DrawerPositions[1][1] +
                InterpTime * (_args.DrawerPositions[0][1] - _args.DrawerPositions[1][1])
            );

            if (InterpTime >= 1)
            {
                EndExecution();
            }
        }

        public void OnConsoleToggled(bool B, ConsoleArgs _args)
        {
            if (B)
                EndExecution();
            else
            {
                BeginExecution();

                // distance along line
                float relx = Mathf.Abs(_args.Drawer.anchoredPosition[1] - _args.DrawerPositions[1][1]);

                // distance of entire line
                float maxx = Mathf.Abs(_args.DrawerPositions[1][1] - _args.DrawerPositions[0][1]);

                InterpTime = relx / maxx;
            }
        }
    }

    [System.Serializable]
    public class ConsoleParse : ConcurrentHeader.ExecutionMachine<ConsoleArgs>.ConcurrentExecution
    {
        [Header("Parser Parameters")]
        [SerializeField] private MonoConsole Console;
        [SerializeField] private float TotalBackspaceThreshold = 0.1F;
        [SerializeField] private float IntervalBackspaceThreshold = 0.035F;

        [System.NonSerialized] private float TotalBackspaceElapsed;
        [System.NonSerialized] private float IntervalBackspaceElapsed;
        [System.NonSerialized] private bool BackspacedLastFrame = false;

        protected override void OnExecutionDiscovery(ConsoleArgs _args)
        {
            RegisterExecution();

            _args.OnConsoleChanged += OnConsoleToggled;
        }
        public override void Simulate(ConsoleArgs _args)
        {
            // pass input into 
            if ((_args.InputFlags & 0x0001) == 0 && // ignore back quote string
                _args.InputString.Length > 0)
                Console?.AppendCommandString(_args.InputString);

            bool _backspace = (_args.InputFlags & 0x0002) != 0;
            bool _remove = false;

            if (_backspace)
            {
                TotalBackspaceElapsed += GlobalTime.DT;
                _remove = !BackspacedLastFrame ||
                (BackspacedLastFrame &&
                TotalBackspaceElapsed > TotalBackspaceThreshold &&
                (IntervalBackspaceElapsed += GlobalTime.DT) > IntervalBackspaceThreshold);
            }
            else
            {
                IntervalBackspaceElapsed = 0F;
                TotalBackspaceElapsed = 0F;
            }

            if (_remove)
            {
                IntervalBackspaceElapsed = 0F;
                Console.RemoveCharacterFromString(1);
            }

            BackspacedLastFrame = _backspace;
        }
        public void OnConsoleToggled(bool B, ConsoleArgs _args)
        {
            if (B)
                BeginExecution();
            else
                EndExecution();
        }
    }

    public class ConsoleBehaviour : ConcurrentHeader.ExecutionMachine<ConsoleArgs>.MonoExecution
    {
        [SerializeField] ConsoleInput InputExecution;
        [SerializeField] ConsoleActivity ActivityExecution;
        [SerializeField] ConsoleEnable EnableExecution;
        [SerializeField] ConsoleDisable DisableExecution;
        [SerializeField] ConsoleParse ParseExecution;

        public override void OnBehaviourDiscovered(
            Action<ConcurrentHeader.ExecutionMachine<ConsoleArgs>.ConcurrentExecution>[] ExecutionCommands,
            ConsoleArgs Middleman)
        {
            InputExecution.OnBaseDiscovery(ExecutionCommands, Middleman);
            ActivityExecution.OnBaseDiscovery(ExecutionCommands, Middleman);
            EnableExecution.OnBaseDiscovery(ExecutionCommands, Middleman);
            DisableExecution.OnBaseDiscovery(ExecutionCommands, Middleman);
            ParseExecution.OnBaseDiscovery(ExecutionCommands, Middleman);
        }
    }
}