using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace pcysl5edgo.Collections.LINQ
{
    public unsafe struct NativeEnumerable<T> : IRefEnumerable<NativeEnumerable<T>.Enumerator, T>, ILinq<T>
        where T : unmanaged
#if STRICT_EQUALITY
        , IEquatable<T>
#endif
    {
        internal readonly T* Ptr;
        internal readonly int Length;

        internal NativeEnumerable(NativeArray<T> array)
        {
            if (array.IsCreated)
            {
                this.Ptr = array.GetPointer();
                this.Length = array.Length;
            }
            else
            {
                this.Ptr = null;
                this.Length = 0;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        #region Enumerable
        public AppendEnumerable<NativeEnumerable<T>, Enumerator, T> Append(T value, Allocator allocator = Allocator.Temp)
            => new AppendEnumerable<NativeEnumerable<T>, Enumerator, T>(this, value, allocator);

        public AppendPointerEnumerable<NativeEnumerable<T>, Enumerator, T> Append(T* value)
            => new AppendPointerEnumerable<NativeEnumerable<T>, Enumerator, T>(this, value);

        public NativeEnumerable<T> AsRefEnumerable() => this;

        public DefaultIfEmptyEnumerable<NativeEnumerable<T>, Enumerator, T> DefaultIfEmpty(T defaultValue, Allocator allocator = Allocator.Temp)
            => new DefaultIfEmptyEnumerable<NativeEnumerable<T>, Enumerator, T>(this, defaultValue, allocator);

        public DistinctEnumerable<NativeEnumerable<T>, Enumerator, T, DefaultEqualityComparer<T>, DefaultGetHashCodeFunc<T>>
            Distinct(Allocator allocator = Allocator.Temp)
            => new DistinctEnumerable<NativeEnumerable<T>, Enumerator, T, DefaultEqualityComparer<T>, DefaultGetHashCodeFunc<T>>(this, default, default, allocator);

        public DistinctEnumerable<NativeEnumerable<T>, Enumerator, T, TEqualityComparer, TGetHashCodeFunc>
            Distinct<TEqualityComparer, TGetHashCodeFunc>
            (TEqualityComparer comparer, TGetHashCodeFunc getHashCodeFunc, Allocator allocator = Allocator.Temp)
            where TEqualityComparer : struct, IRefFunc<T, T, bool>
            where TGetHashCodeFunc : struct, IRefFunc<T, int>
            => new DistinctEnumerable<NativeEnumerable<T>, Enumerator, T, TEqualityComparer, TGetHashCodeFunc>(this, comparer, getHashCodeFunc, allocator);

        public SelectEnumerable<NativeEnumerable<T>, Enumerator, T, TResult, TAction> Select<TResult, TAction>(TAction action, Allocator allocator = Allocator.Temp)
            where TResult : unmanaged
#if STRICT_EQUALITY
            , IEquatable<TResult>
#endif
            where TAction : unmanaged, IRefAction<T, TResult>
            => new SelectEnumerable<NativeEnumerable<T>, Enumerator, T, TResult, TAction>(this, action, allocator);

        public SelectIndexEnumerable<NativeEnumerable<T>, Enumerator, T, TResult, TAction> SelectIndex<TResult, TAction>(TAction action, Allocator allocator = Allocator.Temp)
            where TResult : unmanaged
#if STRICT_EQUALITY
            , IEquatable<TResult>
#endif
            where TAction : unmanaged, ISelectIndex<T, TResult>
            => new SelectIndexEnumerable<NativeEnumerable<T>, Enumerator, T, TResult, TAction>(this, action, allocator);

        public WhereEnumerable<NativeEnumerable<T>, Enumerator, T, TPredicate> Where<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IRefFunc<T, bool>
            => new WhereEnumerable<NativeEnumerable<T>, Enumerator, T, TPredicate>(this, predicate);
        #endregion

        #region Concat
        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                TEnumerable, TEnumerator,
                T
            >
            Concat<TEnumerable, TEnumerator>(in TEnumerable second)
            where TEnumerator : struct, IRefEnumerator<T>
            where TEnumerable : struct, IRefEnumerable<TEnumerator, T>
            => new ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                TEnumerable, TEnumerator, T>(this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                NativeEnumerable<T>,
                Enumerator,
                T
            >
            Concat(in NativeEnumerable<T> second)
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, NativeEnumerable<T>, Enumerator, T>(this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                NativeEnumerable<T>,
                Enumerator,
                T
            >
            Concat(NativeArray<T> second)
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, NativeEnumerable<T>, Enumerator, T>(this, second.AsRefEnumerable());

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                ConcatEnumerable<TEnumerable0, TEnumerator0, TEnumerable1, TEnumerator1, T>,
                ConcatEnumerable<TEnumerable0, TEnumerator0, TEnumerable1, TEnumerator1, T>.Enumerator,
                T
            >
            Concat<TEnumerable0, TEnumerator0, TEnumerable1, TEnumerator1>
            (in ConcatEnumerable<TEnumerable0, TEnumerator0, TEnumerable1, TEnumerator1, T> second)
            where TEnumerator0 : struct, IRefEnumerator<T>
            where TEnumerable0 : struct, IRefEnumerable<TEnumerator0, T>
            where TEnumerator1 : struct, IRefEnumerator<T>
            where TEnumerable1 : struct, IRefEnumerable<TEnumerator1, T>
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, ConcatEnumerable<TEnumerable0, TEnumerator0, TEnumerable1, TEnumerator1, T>, ConcatEnumerable<TEnumerable0, TEnumerator0, TEnumerable1, TEnumerator1, T>.Enumerator, T>(this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                AppendEnumerable<TEnumerable0, TEnumerator0, T>,
                AppendEnumerator<TEnumerator0, T>,
                T
            >
            Concat<TEnumerable0, TEnumerator0>
            (in AppendEnumerable<TEnumerable0, TEnumerator0, T> second)
            where TEnumerator0 : struct, IRefEnumerator<T>
            where TEnumerable0 : struct, IRefEnumerable<TEnumerator0, T>
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, AppendEnumerable<TEnumerable0, TEnumerator0, T>, AppendEnumerator<TEnumerator0, T>, T>(this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                AppendPointerEnumerable<TEnumerable0, TEnumerator0, T>,
                AppendEnumerator<TEnumerator0, T>,
                T
            >
            Concat<TEnumerable0, TEnumerator0>
            (in AppendPointerEnumerable<TEnumerable0, TEnumerator0, T> second)
            where TEnumerator0 : struct, IRefEnumerator<T>
            where TEnumerable0 : struct, IRefEnumerable<TEnumerator0, T>
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, AppendPointerEnumerable<TEnumerable0, TEnumerator0, T>, AppendEnumerator<TEnumerator0, T>, T>(this, second);

#if UNSAFE_ARRAY_ENUMERABLE
        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                ArrayEnumerable<T>,
                ArrayEnumerable<T>.Enumerator,
                T
            >
            Concat(in ArrayEnumerable<T> second)
            => new ConcatEnumerable<
                    NativeEnumerable<T>,
                    Enumerator,
                    ArrayEnumerable<T>,
                    ArrayEnumerable<T>.Enumerator,
                    T
                >
                (this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                ArrayEnumerable<T>,
                ArrayEnumerable<T>.Enumerator,
                T
            >
            Concat(in T[] second)
            => new ConcatEnumerable<
                    NativeEnumerable<T>,
                    Enumerator,
                    ArrayEnumerable<T>,
                    ArrayEnumerable<T>.Enumerator,
                    T
                >
                (this, second.AsRefEnumerable());
#endif

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                DefaultIfEmptyEnumerable<TEnumerable0, TEnumerator0, T>,
                DefaultIfEmptyEnumerable<TEnumerable0, TEnumerator0, T>.Enumerator,
                T
            >
            Concat<TEnumerable0, TEnumerator0>
            (in DefaultIfEmptyEnumerable<TEnumerable0, TEnumerator0, T> second)
            where TEnumerator0 : struct, IRefEnumerator<T>
            where TEnumerable0 : struct, IRefEnumerable<TEnumerator0, T>
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, DefaultIfEmptyEnumerable<TEnumerable0, TEnumerator0, T>, DefaultIfEmptyEnumerable<TEnumerable0, TEnumerator0, T>.Enumerator, T>(this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>, Enumerator,
                DistinctEnumerable<TEnumerable0, TEnumerator0, T, TEqualityComparer, TGetHashCodeFunc>,
                DistinctEnumerable<TEnumerable0, TEnumerator0, T, TEqualityComparer, TGetHashCodeFunc>.Enumerator,
                T
            >
            Concat<TEnumerable0, TEnumerator0, TEqualityComparer, TGetHashCodeFunc>
            (in DistinctEnumerable<TEnumerable0, TEnumerator0, T, TEqualityComparer, TGetHashCodeFunc> second)
            where TEnumerator0 : struct, IRefEnumerator<T>
            where TEnumerable0 : struct, IRefEnumerable<TEnumerator0, T>
            where TEqualityComparer : struct, IRefFunc<T, T, bool>
            where TGetHashCodeFunc : struct, IRefFunc<T, int>
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, DistinctEnumerable<TEnumerable0, TEnumerator0, T, TEqualityComparer, TGetHashCodeFunc>, DistinctEnumerable<TEnumerable0, TEnumerator0, T, TEqualityComparer, TGetHashCodeFunc>.Enumerator, T>(this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>, Enumerator,
                RangeRepeatEnumerable<T, TAction>,
                RangeRepeatEnumerable<T, TAction>.Enumerator,
                T
            >
            Concat<TAction>
            (in RangeRepeatEnumerable<T, TAction> second)
            where TAction : struct, IRefAction<T>
            => new ConcatEnumerable<
                    NativeEnumerable<T>, Enumerator,
                    RangeRepeatEnumerable<T, TAction>,
                    RangeRepeatEnumerable<T, TAction>.Enumerator,
                    T
                >
                (this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                SelectIndexEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction>,
                SelectIndexEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction>.Enumerator,
                T
            >
            Concat<TEnumerable0, TEnumerator0, TPrevSource, TAction>
            (in SelectIndexEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction> second)
            where TEnumerator0 : struct, IRefEnumerator<TPrevSource>
            where TEnumerable0 : struct, IRefEnumerable<TEnumerator0, TPrevSource>
            where TPrevSource : unmanaged
#if STRICT_EQUALITY
            , IEquatable<TPrevSource>
#endif
            where TAction : unmanaged, ISelectIndex<TPrevSource, T>
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, SelectIndexEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction>, SelectIndexEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction>.Enumerator, T>(this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                SelectEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction>,
                SelectEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction>.Enumerator,
                T
            >
            Concat<TEnumerable0, TEnumerator0, TPrevSource, TAction>
            (in SelectEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction> second)
            where TEnumerator0 : struct, IRefEnumerator<TPrevSource>
            where TEnumerable0 : struct, IRefEnumerable<TEnumerator0, TPrevSource>
            where TPrevSource : unmanaged
#if STRICT_EQUALITY
            , IEquatable<TPrevSource>
#endif
            where TAction : unmanaged, IRefAction<TPrevSource, T>
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, SelectEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction>, SelectEnumerable<TEnumerable0, TEnumerator0, TPrevSource, T, TAction>.Enumerator, T>(this, second);

        public ConcatEnumerable<
                NativeEnumerable<T>,
                Enumerator,
                WhereEnumerable<TEnumerable0, TEnumerator0, T, TPredicate>,
                WhereEnumerable<TEnumerable0, TEnumerator0, T, TPredicate>.Enumerator,
                T
            >
            Concat<TEnumerable0, TEnumerator0, TPredicate>
            (in WhereEnumerable<TEnumerable0, TEnumerator0, T, TPredicate> second)
            where TEnumerator0 : struct, IRefEnumerator<T>
            where TEnumerable0 : struct, IRefEnumerable<TEnumerator0, T>
            where TPredicate : unmanaged, IRefFunc<T, bool>
            => new ConcatEnumerable<NativeEnumerable<T>, Enumerator, WhereEnumerable<TEnumerable0, TEnumerator0, T, TPredicate>, WhereEnumerable<TEnumerable0, TEnumerator0, T, TPredicate>.Enumerator, T>(this, second);
        #endregion

        #region Function
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanFastCount() => true;

        void ILinq<T>.Aggregate<TAccumulate, TFunc>(ref TAccumulate seed, TFunc func)
        {
            for (var i = 0; i < Length; i++)
                func.Execute(ref seed, ref Ptr[i]);
        }

        TResult ILinq<T>.Aggregate<TAccumulate, TResult, TFunc, TResultFunc>(ref TAccumulate seed, TFunc func, TResultFunc resultFunc)
        {
            for (var i = 0; i < Length; i++)
                func.Execute(ref seed, ref Ptr[i]);
            return resultFunc.Calc(ref seed);
        }

        public T Aggregate(Func<T, T, T> func)
        {
            if (Length == 0) throw new InvalidOperationException();
            var seed = Ptr[0];
            for (var i = 1L; i < Length; i++)
                seed = func(seed, Ptr[i]);
            return seed;
        }

        TAccumulate ILinq<T>.Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
        {
            for (var i = 0; i < Length; i++)
                seed = func(seed, Ptr[i]);
            return seed;
        }

        TResult ILinq<T>.Aggregate<TAccumulate, TResult>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, Func<TAccumulate, TResult> resultFunc)
        {
            for (var i = 0; i < Length; i++)
                seed = func(seed, Ptr[i]);
            return resultFunc(seed);
        }

        bool ILinq<T>.All<TPredicate>(TPredicate predicate)
        {
            for (var i = 0; i < Length; i++)
                if (!predicate.Calc(ref Ptr[i]))
                    return false;
            return true;
        }

        bool ILinq<T>.All(Func<T, bool> predicate)
        {
            for (var i = 0; i < Length; i++)
                if (!predicate(Ptr[i]))
                    return false;
            return true;
        }

        public bool Any() => Length != 0;

        bool ILinq<T>.Any<TPredicate>(TPredicate predicate)
        {
            for (var i = 0; i < Length; i++)
                if (predicate.Calc(ref Ptr[i]))
                    return true;
            return false;
        }

        bool ILinq<T>.Any(Func<T, bool> predicate)
        {
            for (var i = 0; i < Length; i++)
                if (predicate(Ptr[i]))
                    return true;
            return false;
        }

        bool ILinq<T>.Contains(T value)
        {
            for (var i = 0; i < Length; i++)
                if (Ptr[i].Equals(value))
                    return true;
            return false;
        }

        bool ILinq<T>.Contains(T value, IEqualityComparer<T> comparer)
        {
            for (var i = 0; i < Length; i++)
                if (comparer.Equals(value, Ptr[i]))
                    return true;
            return false;
        }

        bool ILinq<T>.Contains<TComparer>(T value, TComparer comparer)
        {
            for (var i = 0; i < Length; i++)
                if (comparer.Calc(ref Ptr[i], ref value))
                    return true;
            return false;
        }

        public int Count() => this.Length;

        public int Count(Func<T, bool> predicate)
        {
            var count = 0;
            for (var i = 0; i < Length; i++)
                if (predicate(Ptr[i]))
                    ++count;
            return count;
        }

        public int Count<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IRefFunc<T, bool>
        {
            var count = 0;
            for (var i = 0; i < Length; i++)
                if (predicate.Calc(ref Ptr[i]))
                    ++count;
            return count;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public long LongCount() => this.Length;

        long ILinq<T>.LongCount(Func<T, bool> predicate) => Count(predicate);

        long ILinq<T>.LongCount<TPredicate>(TPredicate predicate) => Count(predicate);

        T[] ILinq<T>.ToArray()
        {
            if (Length <= 0) return Array.Empty<T>();
            var answer = new T[Length];
            fixed (T* p = &answer[0])
                UnsafeUtility.MemCpy(p, Ptr, sizeof(T) * Length);
            return answer;
        }

        Dictionary<TKey, TElement> ILinq<T>.ToDictionary<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector)
        {
            var answer = new Dictionary<TKey, TElement>(Length);
            for (var i = 0; i < Length; i++)
                answer.Add(keySelector(Ptr[i]), elementSelector(Ptr[i]));
            return answer;
        }

        Dictionary<TKey, TElement> ILinq<T>.ToDictionary<TKey, TElement, TKeyFunc, TElementFunc>(TKeyFunc keySelector, TElementFunc elementSelector)
        {
            var answer = new Dictionary<TKey, TElement>(Length);
            for (var i = 0; i < Length; i++)
            {
                ref var item = ref Ptr[i];
                answer.Add(keySelector.Calc(ref item), elementSelector.Calc(ref item));
            }
            return answer;
        }

        HashSet<T> ILinq<T>.ToHashSet()
        {
            var answer = new HashSet<T>();
            for (var i = 0; i < Length; i++)
                answer.Add(Ptr[i]);
            return answer;
        }

        HashSet<T> ILinq<T>.ToHashSet(IEqualityComparer<T> comparer)
        {
            var answer = new HashSet<T>(comparer);
            for (var i = 0; i < Length; i++)
                answer.Add(Ptr[i]);
            return answer;
        }

        List<T> ILinq<T>.ToList()
        {
            var answer = new List<T>(Length);
            for (var i = 0; i < Length; i++)
                answer.Add(Ptr[i]);
            return answer;
        }

        NativeArray<T> ILinq<T>.ToNativeArray(Allocator allocator)
        {
            if (Length == 0) return default;
            var answer = new NativeArray<T>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(answer.GetPointer(), Ptr, sizeof(T) * Length);
            return answer;
        }

        bool ILinq<T>.TryGetElementAt(int index, out T element)
        {
            if (index < 0 || index >= Length)
            {
                element = default;
                return false;
            }
            element = Ptr[index];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetFirst(out T first)
        {
            if (Length == 0)
            {
                first = default;
                return false;
            }
            first = *Ptr;
            return true;
        }

        public bool TryGetLast(out T last)
        {
            if (Length == 0)
            {
                last = default;
                return false;
            }
            last = Ptr[Length - 1];
            return true;
        }

        bool ILinq<T>.TryGetSingle(out T value)
        {
            if (Length != 1)
            {
                value = default;
                return false;
            }
            value = *Ptr;
            return true;
        }

        bool ILinq<T>.TryGetSingle<TPredicate>(out T value, TPredicate predicate)
            => this.TryGetSingle<NativeEnumerable<T>, Enumerator, T, TPredicate>(out value, predicate);

        bool ILinq<T>.TryGetSingle(out T value, Func<T, bool> predicate)
            => this.TryGetSingle<NativeEnumerable<T>, Enumerator, T>(out value, predicate);
        #endregion

        public struct Enumerator : IRefEnumerator<T>
        {
            private readonly T* ptr;
            private readonly int length;
            private int index;

            public ref T Current => ref ptr[index];
            T IEnumerator<T>.Current => Current;
            object IEnumerator.Current => Current;

            internal Enumerator(in NativeEnumerable<T> parent)
            {
                index = -1;
                ptr = parent.Ptr;
                length = parent.Length;
            }

            public void Dispose() => this = default;

            public bool MoveNext() => ++index < length;

            public void Reset() => throw new InvalidOperationException();
        }
    }
}