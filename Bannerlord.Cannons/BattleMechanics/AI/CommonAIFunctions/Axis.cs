using System;
using System.Collections.Generic;
using System.Linq;

namespace Bannerlord.Cannons.BattleMechanics.AI.CommonAIFunctions
{
    /// <summary>
    /// A single scoring dimension used by target selectors.
    ///
    /// <typeparam name="T">
    /// The type of object being evaluated — typically <see cref="Target"/> in production
    /// code, but kept generic so the axis math can be unit-tested without engine types.
    /// </typeparam>
    ///
    /// Each axis maps one property of a <typeparamref name="T"/> (e.g. distance,
    /// unit count) to a score in [0, 1] through two steps:
    /// <list type="number">
    ///   <item><description>
    ///     The <i>parameter function</i> extracts a raw float from the object
    ///     (e.g. formation.CountOfUnits = 45).
    ///   </description></item>
    ///   <item><description>
    ///     The raw value is normalised into [0, 1] against the axis min/max,
    ///     then passed through the <i>output function</i> (which may be linear,
    ///     cubic, etc.) and finally clamped to [0, 1].
    ///   </description></item>
    /// </list>
    ///
    /// An optional <i>activation function</i> can disable the axis entirely for
    /// objects where it does not apply (e.g. a siege-weapon-specific axis should
    /// not fire for formation targets).
    /// </summary>
    public class Axis<T>
    {
        private readonly float _min;
        private readonly float _max;
        private readonly float _range;

        private readonly Func<float, float> _outputFunction;
        private readonly Func<T, float> _parameterFunction;
        private readonly Func<T, bool>? _activationFunction;

        /// <param name="minInput">Raw input value that maps to normalised 0.</param>
        /// <param name="maxInput">Raw input value that maps to normalised 1.</param>
        /// <param name="outputFunction">
        ///   Maps a normalised input [0, 1] to a score. The result is clamped to
        ///   [0, 1] regardless of what this function returns.
        /// </param>
        /// <param name="parameterFunction">Extracts the raw input value from the object.</param>
        /// <param name="activationFunction">
        ///   When provided, the axis is skipped (treated as inactive) for any object
        ///   where this returns <c>false</c>. Leave <c>null</c> to always activate.
        /// </param>
        public Axis(
            float minInput,
            float maxInput,
            Func<float, float> outputFunction,
            Func<T, float> parameterFunction,
            Func<T, bool>? activationFunction = null)
        {
            _min = minInput;
            _max = maxInput;
            _range = maxInput - minInput;
            _outputFunction = outputFunction;
            _parameterFunction = parameterFunction;
            _activationFunction = activationFunction;
        }

        /// <summary>
        /// Evaluates this axis for the given object and returns a score in [0, 1].
        /// </summary>
        public float Evaluate(T target)
        {
            float rawInput   = _parameterFunction.Invoke(target);
            float normalised = (Math.Max(_min, Math.Min(_max, rawInput)) - _min) / _range;
            float rawScore   = _outputFunction.Invoke(normalised);
            return Math.Max(0f, Math.Min(1f, rawScore));
        }

        /// <summary>
        /// Returns <c>true</c> when this axis should be included in scoring for the
        /// given object. Always returns <c>true</c> if no activation function was set.
        /// </summary>
        public bool IsActive(T target)
            => _activationFunction == null || _activationFunction.Invoke(target);
    }

    /// <summary>
    /// Convenience alias: <c>Axis</c> is <c>Axis&lt;Target&gt;</c> for production code
    /// that works with artillery <see cref="Target"/> objects.
    /// </summary>
    public class Axis : Axis<Target>
    {
        /// <inheritdoc cref="Axis{T}(float,float,Func{float,float},Func{T,float},Func{T,bool})"/>
        public Axis(
            float minInput,
            float maxInput,
            Func<float, float> outputFunction,
            Func<Target, float> parameterFunction,
            Func<Target, bool>? activationFunction = null)
            : base(minInput, maxInput, outputFunction, parameterFunction, activationFunction)
        {
        }
    }

    /// <summary>
    /// Extension methods for combining multiple <see cref="Axis{T}"/> evaluations
    /// into a single composite score.
    /// </summary>
    public static class AxisExtensions
    {
        /// <summary>
        /// Computes the geometric mean of all active axes for the given object.
        ///
        /// The geometric mean (n-th root of the product) is preferred over the
        /// arithmetic mean because it is <b>multiplicatively coupled</b>: any axis
        /// that scores near zero drags the whole result toward zero. This prevents
        /// a target that is excellent on three axes but terrible on one (e.g.
        /// enormous range) from still scoring well overall.
        ///
        /// Returns 0 if no axes are active.
        /// </summary>
        public static float GeometricMean<T>(this List<Axis<T>> axes, T target)
        {
            var activeAxes = axes.FindAll(axis => axis.IsActive(target));
            if (!activeAxes.Any()) return 0f;

            var evaluations = activeAxes.Select(axis => axis.Evaluate(target)).ToList();
            return (float)Math.Pow(evaluations.Aggregate((a, x) => a * x), 1.0 / activeAxes.Count);
        }

        /// <summary>
        /// Computes the arithmetic mean of all active axes for the given object.
        /// Returns 0 if no axes are active.
        /// </summary>
        public static float ArithmeticMean<T>(this List<Axis<T>> axes, T target)
        {
            var activeAxes = axes.FindAll(axis => axis.IsActive(target));
            if (!activeAxes.Any()) return 0f;

            var evaluations = activeAxes.Select(axis => axis.Evaluate(target)).ToList();
            return evaluations.Aggregate((a, x) => a + x) / activeAxes.Count;
        }
    }
}
