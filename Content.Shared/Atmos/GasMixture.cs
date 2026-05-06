using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Reactions;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos
{
    // Статический класс для extension методов (ДОБАВИТЬ ЭТОТ КЛАСС)
    public static class GasMixtureExtensions
    {
        public static GasMixture WithWater(this GasMixture mixture)
        {
            mixture.AdjustMoles(Gas.Water, 1000f);
            // Добавляем настройку давления через моли
            // P*V = n*R*T => n = (P*V)/(R*T)
            // При P=500, V=2500, R=8.314, T возьмем среднее ~260K
            // n = (500 * 2500) / (8.314 * 260) ≈ 578 моль
            mixture.SetMoles(Gas.Water, 578f);
            mixture.Temperature = 260f; // -13°C, среднее между -26 и 0
            return mixture;
        }
    }

    /// <summary>
    ///     A general-purpose, variable volume gas mixture.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class GasMixture : IEquatable<GasMixture>, ISerializationHooks, IEnumerable<(Gas gas, float moles)>
    {
        public static GasMixture SpaceGas => new() { Volume = Atmospherics.CellVolume, Temperature = Atmospherics.TCMB, Immutable = true };

        // ИСПРАВЛЕНО: Теперь использует extension метод из статического класса
        public static GasMixture SpaceWater
        {
            get
            {
                // Для P = 1000 kPa, V = 2500 L, T = 273.15 K
                // n = (P * V) / (R * T) = (1000 * 2500) / (8.314 * 273.15) ≈ 1100.5 моль
                const float targetPressure = 1000f; // kPa
                const float targetTemp = Atmospherics.T0C; // 273.15 K
                var volume = Atmospherics.CellVolume; // 2500 L

                var requiredMoles = (targetPressure * volume) / (Atmospherics.R * targetTemp);

                var mixture = new GasMixture(volume)
                {
                    Temperature = targetTemp,
                    Immutable = true
                };
                mixture.SetMoles(Gas.Water, requiredMoles);
                return mixture;
            }
        }
        // ИСПРАВЛЕНО: Убираем метод расширения отсюда, он теперь в статическом классе выше
        // public static GasMixture WithWater(this GasMixture mixture) {mixture.AdjustMoles(Gas.Water, 1000f);  return mixture;}

        // No access, to ensure immutable mixtures are never accidentally mutated.
        [Access(typeof(SharedAtmosphereSystem), typeof(SharedAtmosDebugOverlaySystem), typeof(GasEnumerator), Other = AccessPermissions.None)]
        [DataField(customTypeSerializer: typeof(GasArraySerializer))]
        public float[] Moles = new float[Atmospherics.AdjustedNumberOfGases];

        public float this[int gas] => Moles[gas];

        [DataField("temperature")]
        [ViewVariables(VVAccess.ReadWrite)]
        private float _temperature = Atmospherics.TCMB;

        [DataField("immutable")]
        public bool Immutable { get; private set; }

        [ViewVariables]
        public readonly float[] ReactionResults =
        {
            0f,
        };

        [ViewVariables]
        public float TotalMoles
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => NumericsHelpers.HorizontalAdd(Moles);
        }

        [ViewVariables]
        public float Pressure
        {
            get
            {
                if (Volume <= 0) return 0f;
                return TotalMoles * Atmospherics.R * Temperature / Volume;
            }
        }

        [ViewVariables]
        public float Temperature
        {
            get => _temperature;
            set
            {
                DebugTools.Assert(!float.IsNaN(value));
                if (!Immutable)
                    _temperature = MathF.Min(MathF.Max(value, Atmospherics.TCMB), Atmospherics.Tmax);
            }
        }

        [DataField("volume")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Volume { get; set; }

        public GasMixture()
        {
        }

        public GasMixture(float volume = 0f)
        {
            if (volume < 0)
                volume = 0;
            Volume = volume;
        }

        public GasMixture(float[] moles, float temp, float volume = Atmospherics.CellVolume)
        {
            if (moles.Length != Atmospherics.AdjustedNumberOfGases)
                throw new InvalidOperationException($"Invalid mole array length");

            if (volume < 0)
                volume = 0;

            DebugTools.Assert(!float.IsNaN(temp));
            _temperature = temp;
            Moles = moles;
            Volume = volume;
        }

        public GasMixture(GasMixture toClone)
        {
            CopyFrom(toClone);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkImmutable()
        {
            Immutable = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMoles(int gasId)
        {
            return Moles[gasId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMoles(Gas gas)
        {
            return GetMoles((int)gas);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMoles(int gasId, float quantity)
        {
            if (!float.IsFinite(quantity) || float.IsNegative(quantity))
                throw new ArgumentException($"Invalid quantity \"{quantity}\" specified!", nameof(quantity));

            if (!Immutable)
                Moles[gasId] = quantity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMoles(Gas gas, float quantity)
        {
            SetMoles((int)gas, quantity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdjustMoles(int gasId, float quantity)
        {
            if (Immutable)
                return;

            if (!float.IsFinite(quantity))
                throw new ArgumentException($"Invalid quantity \"{quantity}\" specified!", nameof(quantity));

            // Clamping is needed because x - x can be negative with floating point numbers. If we don't
            // clamp here, the caller always has to call GetMoles(), clamp, then SetMoles().
            ref var moles = ref Moles[gasId];
            moles = MathF.Max(moles + quantity, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdjustMoles(Gas gas, float moles)
        {
            AdjustMoles((int)gas, moles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasMixture Remove(float amount)
        {
            return RemoveRatio(amount / TotalMoles);
        }

        // Content.Shared/Atmos/GasMixture.cs
        // Найти метод RemoveRatio и заменить его

        public GasMixture RemoveRatio(float ratio)
        {
            switch (ratio)
            {
                case <= 0:
                    return new GasMixture(Volume) { Temperature = Temperature };
                case > 1:
                    ratio = 1;
                    break;
            }

            var removed = new GasMixture(Volume) { Temperature = Temperature };

            // Сохраняем воду из оригинальной смеси
            var originalWater = Moles[(int)Gas.Water];
            var originalLiquidWater = Moles[(int)Gas.LiquidWater];

            Moles.CopyTo(removed.Moles.AsSpan());
            NumericsHelpers.Multiply(removed.Moles, ratio);
            if (!Immutable)
                NumericsHelpers.Sub(Moles, removed.Moles);

            // Восстанавливаем воду в исходной смеси
            if (!Immutable)
            {
                if (originalWater > 0)
                {
                    Moles[(int)Gas.Water] = originalWater;
                    removed.Moles[(int)Gas.Water] = 0;
                }

                if (originalLiquidWater > 0)
                {
                    Moles[(int)Gas.LiquidWater] = originalLiquidWater;
                    removed.Moles[(int)Gas.LiquidWater] = 0;
                }
            }

            for (var i = 0; i < Moles.Length; i++)
            {
                var moles = Moles[i];
                var otherMoles = removed.Moles[i];

                if ((moles < Atmospherics.GasMinMoles || float.IsNaN(moles)) && !Immutable)
                    Moles[i] = 0;

                if (otherMoles < Atmospherics.GasMinMoles || float.IsNaN(otherMoles))
                    removed.Moles[i] = 0;
            }

            return removed;
        }

        public GasMixture RemoveVolume(float vol)
        {
            return RemoveRatio(vol / Volume);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(GasMixture sample)
        {
            if (Immutable)
                return;

            Volume = sample.Volume;
            sample.Moles.CopyTo(Moles, 0);
            Temperature = sample.Temperature;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (Immutable) return;

            // -- МОДИФИЦИРУЕМ ЭТОТ КОД --
            // Сохраняем воду, если она есть
            var savedWater = Moles[(int)Gas.Water];
            Array.Clear(Moles, 0, Atmospherics.TotalNumberOfGases);

            // Восстанавливаем воду
            if (savedWater > 0)
            {
                Moles[(int)Gas.Water] = savedWater;
                // Не восстанавливаем температуру, чтобы вода могла нагреваться
            }
            // ---------------------------
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(float multiplier)
        {
            if (Immutable) return;
            NumericsHelpers.Multiply(Moles, multiplier);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // ISerializationHooks is obsolete.
            // TODO add fixed-length-array serializer

            // The arrays MUST have a specific length.
            Array.Resize(ref Moles, Atmospherics.AdjustedNumberOfGases);
        }

        public GasMixtureStringRepresentation ToPrettyString()
        {
            var molesPerGas = new Dictionary<string, float>();
            for (int i = 0; i < Moles.Length; i++)
            {
                if (Moles[i] == 0)
                    continue;

                molesPerGas.Add(((Gas)i).ToString(), Moles[i]);
            }

            return new GasMixtureStringRepresentation(TotalMoles, Temperature, Pressure, molesPerGas);
        }

        GasEnumerator GetEnumerator()
        {
            return new GasEnumerator(this);
        }

        IEnumerator<(Gas gas, float moles)> IEnumerable<(Gas gas, float moles)>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object? obj)
        {
            if (obj is GasMixture mix)
                return Equals(mix);
            return false;
        }

        public bool Equals(GasMixture? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (ReferenceEquals(null, other))
                return false;

            return Moles.SequenceEqual(other.Moles)
                   && _temperature.Equals(other._temperature)
                   && ReactionResults.SequenceEqual(other.ReactionResults)
                   && Immutable == other.Immutable
                   && Volume.Equals(other.Volume);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var moles = Moles[i];
                hashCode.Add(moles);
            }

            hashCode.Add(_temperature);
            hashCode.Add(Immutable);
            hashCode.Add(Volume);

            return hashCode.ToHashCode();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public GasMixture Clone()
        {
            if (Immutable)
                return this;

            var newMixture = new GasMixture()
            {
                Moles = (float[])Moles.Clone(),
                _temperature = _temperature,
                Volume = Volume,
            };
            return newMixture;
        }

        public struct GasEnumerator(GasMixture mixture) : IEnumerator<(Gas gas, float moles)>
        {
            private int _idx = -1;

            public void Dispose()
            {
                // Nada.
            }

            public bool MoveNext()
            {
                return ++_idx < Atmospherics.TotalNumberOfGases;
            }

            public void Reset()
            {
                _idx = -1;
            }

            public (Gas gas, float moles) Current => ((Gas)_idx, mixture.Moles[_idx]);
            object? IEnumerator.Current => Current;
        }
    }
}
