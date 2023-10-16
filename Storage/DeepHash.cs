using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Fury.Storage
{
    public class HashCode
    {
        private System.HashCode _h = new System.HashCode();

        public void Add<T>(T value)
        {
            _h.Add(value);
        }

        public int ToHashCode()
        {
            return _h.ToHashCode();
        }
    }

    public static class DeepHash
    {
        static readonly MethodInfo hashcode_AddT = typeof(HashCode)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "Add" && m.GetParameters().Length == 1)
            .FirstOrDefault() ?? throw new Exception("Not found");

        static readonly MethodInfo enumerator_MoveNext = typeof(IEnumerator)
            .GetMethod("MoveNext") ?? throw new Exception("Not found");

        static readonly MethodInfo deephash_AggregateT = typeof(DeepHash)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(Aggregate)) ?? throw new Exception("Not found");

        public static int GetHashCode<T>(T target)
        {
            var hashcode = new HashCode();
            Aggregate<T>(hashcode, target);
            return hashcode.ToHashCode();
        }

        private delegate void HashAggregateDelegate<T>(HashCode hashcode, T target);

        static readonly Dictionary<Type, Delegate> _aggregators
            = new Dictionary<Type, Delegate>();

        private static void Aggregate<T>(HashCode hashcode, T target)
        {
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            {
                hashcode.Add(target);
            }
            else
            {
                if (!_aggregators.TryGetValue(typeof(T), out var agg))
                {
                    agg = CreateAggreagtor<T>();
                    _aggregators.Add(typeof(T), agg);
                }
                ((HashAggregateDelegate<T>)agg).Invoke(hashcode, target);
            }
        }

        private static HashAggregateDelegate<T> CreateAggreagtor<T>()
        {
            var type = typeof(T);
            if (!type.IsClass && !type.IsValueType)
            {
                throw new ArgumentException("Only for class or structs");
            }
            var hascode = Expression.Parameter(typeof(HashCode), "hascode");
            var target = Expression.Parameter(typeof(T), "target");

            var variables = new List<ParameterExpression>();
            var body = new List<Expression>();
            foreach (var fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var field = Expression.Field(target, fieldInfo);
                var fieldType = fieldInfo.FieldType;
                if (fieldType.IsPrimitive || fieldType == typeof(string))
                {
                    var add = hashcode_AddT.MakeGenericMethod(fieldType);
                    body.Add(Expression.Call(hascode, add, field));
                } else if (GetImplementation(fieldType, typeof(IEnumerable<>), out var enumeretableType))
                {
                    var itemType = enumeretableType.GetGenericArguments()[0];
                    var enumeratorType = typeof(IEnumerator<>).MakeGenericType(itemType);
                    var enumerator = Expression.Variable(
                        enumeratorType,
                        $"enumerator_{fieldInfo.Name}");

                    variables.Add(enumerator);
                    body.Add(Expression.Assign(
                        enumerator,
                        Expression.Call(field, enumeretableType.GetMethod("GetEnumerator"))));

                    var aggregate = deephash_AggregateT.MakeGenericMethod(itemType);
                    var current = Expression.Property(enumerator, enumeratorType.GetProperty("Current"));

                    var @break = Expression.Label();
                    var loopBody = Expression.IfThenElse(
                        Expression.Not(Expression.Call(enumerator, enumerator_MoveNext)),
                        Expression.Break(@break),
                        Expression.Call(null, aggregate, hascode, current));

                    body.Add(Expression.Loop(loopBody, @break));
                } else
                {
                    var aggregate = deephash_AggregateT.MakeGenericMethod(fieldType);
                    body.Add(Expression.Call(null, aggregate, hascode, field));
                }
            }

            return Expression
                .Lambda<HashAggregateDelegate<T>>(Expression.Block(variables, body), hascode, target)
                .Compile();
        }

        private static bool GetImplementation(Type obj, Type generic, out Type implementation)
        {
            foreach (var i in obj.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == generic)
                {
                    implementation = i;
                    return true;
                }
            }
            implementation = default;
            return false;
        }
    }
}