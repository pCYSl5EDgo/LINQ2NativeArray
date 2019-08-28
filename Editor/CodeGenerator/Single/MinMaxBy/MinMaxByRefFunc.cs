﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
// ReSharper disable InconsistentNaming

namespace UniNativeLinq.Editor.CodeGenerator.MinMaxBy
{
    public sealed class MinMaxByRefFunc : ITypeDictionaryHolder, IApiExtensionMethodGenerator
    {
        public MinMaxByRefFunc(ISingleApi api, bool isMax, string keyName)
        {
            Api = api;
            this.isMax = isMax;
            this.keyName = keyName;
        }

        private readonly bool isMax;
        private readonly string keyName;
        private string Name => isMax ? "MaxBy" : "MinBy";
        public readonly ISingleApi Api;
        public Dictionary<string, TypeDefinition> Dictionary { private get; set; }
        public void Generate(IEnumerableCollectionProcessor processor, ModuleDefinition mainModule, ModuleDefinition systemModule, ModuleDefinition unityModule)
        {
            var array = processor.EnabledNameCollection.Intersect(Api.NameCollection).ToArray();
            if (!Api.ShouldDefine(array)) return;
            TypeDefinition @static;
            mainModule.Types.Add(@static = mainModule.DefineStatic(Name + "RefFunc" + keyName + "Helper"));
            TypeReference keyType;
            switch (keyName)
            {
                case "Double":
                    keyType = mainModule.TypeSystem.Double;
                    break;
                case "Single":
                    keyType = mainModule.TypeSystem.Single;
                    break;
                case "Int32":
                    keyType = mainModule.TypeSystem.Int32;
                    break;
                case "UInt32":
                    keyType = mainModule.TypeSystem.UInt32;
                    break;
                case "Int64":
                    keyType = mainModule.TypeSystem.Int64;
                    break;
                case "UInt64":
                    keyType = mainModule.TypeSystem.UInt64;
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
            foreach (var name in array)
            {
                if (!processor.IsSpecialType(name, out var isSpecial)) throw new KeyNotFoundException();
                if (!Api.TryGetEnabled(name, out var apiEnabled) || !apiEnabled) continue;
                GenerateEach(name, isSpecial, @static, mainModule, systemModule, keyType);
            }
        }

        private void GenerateEach(string name, bool isSpecial, TypeDefinition @static, ModuleDefinition mainModule, ModuleDefinition systemModule, TypeReference keyType)
        {
            var returnTypeDefinition = mainModule.GetType("UniNativeLinq", Name + keyName + "Enumerable`4");

            var method = new MethodDefinition(Name, Helper.StaticMethodAttributes, mainModule.TypeSystem.Boolean)
            {
                DeclaringType = @static,
                AggressiveInlining = true,
                CustomAttributes = { Helper.ExtensionAttribute }
            };
            @static.Methods.Add(method);

            var T = new GenericParameter("T", method)
            {
                HasNotNullableValueTypeConstraint = true,
                CustomAttributes = { Helper.GetSystemRuntimeInteropServicesUnmanagedTypeConstraintTypeReference() }
            };
            method.GenericParameters.Add(T);

            var func = new GenericInstanceType(mainModule.GetType("UniNativeLinq", "RefFunc`2"))
            {
                GenericArguments = { T, keyType }
            };

            var TKeySelector = new GenericInstanceType(mainModule.GetType("UniNativeLinq", "DelegateRefFuncToStructOperatorFunc`2"))
            {
                GenericArguments = { T, keyType }
            };

            if (isSpecial)
            {
                var (baseEnumerable, enumerable, enumerator) = T.MakeSpecialTypePair(name);
                method.ReturnType = new GenericInstanceType(returnTypeDefinition)
                {
                    GenericArguments = { enumerable, enumerator, T, TKeySelector }
                };
                switch (name)
                {
                    case "T[]":
                    case "NativeArray<T>":
                        GenerateSpecial(method, baseEnumerable, enumerable, func, TKeySelector);
                        break;
                    default: throw new NotSupportedException(name);
                }
            }
            else
            {
                var type = Dictionary[name];
                var (enumerable, enumerator, _) = T.MakeFromCommonType(method, type, "0");
                method.ReturnType = new GenericInstanceType(returnTypeDefinition)
                {
                    GenericArguments = { enumerable, enumerator, T, TKeySelector }
                };
                GenerateNormal(method, enumerable, func, TKeySelector);
            }
        }

        private static void GenerateNormal(MethodDefinition method, TypeReference enumerable, TypeReference func, TypeReference TKeySelector)
        {
            method.Parameters.Add(new ParameterDefinition("@this", ParameterAttributes.In, new ByReferenceType(enumerable))
            {
                CustomAttributes = { Helper.GetSystemRuntimeCompilerServicesReadonlyAttributeTypeReference() }
            });
            method.Parameters.Add(new ParameterDefinition("keySelector", ParameterAttributes.None, func));
            var allocator = new ParameterDefinition("allocator", ParameterAttributes.HasDefault | ParameterAttributes.Optional, Helper.Allocator)
            {
                Constant = 2,
            };
            method.Parameters.Add(allocator);

            var body = method.Body;

            body.Variables.Add(new VariableDefinition(TKeySelector));

            body.GetILProcessor()
                .LdArg(0)
                .LdArg(1)
                .StLoc(0)
                .LdLocA(0)
                .LdArg(2)
                .NewObj(method.ReturnType.FindMethod(".ctor", 3))
                .Ret();
        }

        private static void GenerateSpecial(MethodDefinition method, TypeReference baseEnumerable, GenericInstanceType enumerable, TypeReference func, TypeReference TKeySelector)
        {
            method.Parameters.Add(new ParameterDefinition("@this", ParameterAttributes.None, baseEnumerable));
            method.Parameters.Add(new ParameterDefinition("keySelector", ParameterAttributes.None, func));
            var allocator = new ParameterDefinition("allocator", ParameterAttributes.HasDefault | ParameterAttributes.Optional, Helper.Allocator)
            {
                Constant = 2,
            };
            method.Parameters.Add(allocator);

            var body = method.Body;
            body.InitLocals = true;
            body.Variables.Add(new VariableDefinition(enumerable));
            body.Variables.Add(new VariableDefinition(TKeySelector));

            body.GetILProcessor()
                .LdLocA(0)
                .LdArg(0)
                .Call(enumerable.FindMethod(".ctor", 1))

                .LdArg(1)
                .StLoc(1)

                .LdLocA(0)
                .LdLocA(1)
                .LdArg(2)
                .NewObj(method.ReturnType.FindMethod(".ctor", 3))
                .Ret();
        }
    }
}