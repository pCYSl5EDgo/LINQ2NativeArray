﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
// ReSharper disable InconsistentNaming

namespace UniNativeLinq.Editor.CodeGenerator
{
    /*
        TResult Aggregate<TAccumulate, TResult>(ref TAccumulate seed, RefAction<T, TAccumulate> func, RefFunc<TAccumulate, TResult> resultFunc)
    */
    public sealed class AggregateRefValue2Refs : ITypeDictionaryHolder, IApiExtensionMethodGenerator
    {
        public AggregateRefValue2Refs(ISingleApi api)
        {
            Api = api;
        }

        public readonly ISingleApi Api;
        public Dictionary<string, TypeDefinition> Dictionary { private get; set; }

        public void Generate(IEnumerableCollectionProcessor processor, ModuleDefinition mainModule, ModuleDefinition systemModule, ModuleDefinition unityModule)
        {
            var array = processor.EnabledNameCollection.Intersect(Api.NameCollection).ToArray();
            if (!Api.ShouldDefine(array)) return;
            TypeDefinition @static;
            mainModule.Types.Add(@static = mainModule.DefineStatic(nameof(AggregateRefValue2Refs) + "Helper"));

            if (Api.TryGetEnabled("TEnumerable", out var genericEnabled) && genericEnabled)
                GenerateGeneric(@static, mainModule);

            foreach (var name in array)
            {
                if (!processor.IsSpecialType(name, out var isSpecial)) throw new KeyNotFoundException();
                if (!Api.TryGetEnabled(name, out var apiEnabled) || !apiEnabled) continue;
                GenerateEach(name, isSpecial, @static, mainModule);
            }
        }

        private void GenerateEach(string name, bool isSpecial, TypeDefinition @static, ModuleDefinition mainModule)
        {
            var method = new MethodDefinition("Aggregate", Helper.StaticMethodAttributes, mainModule.TypeSystem.Boolean)
            {
                DeclaringType = @static,
                AggressiveInlining = true,
                CustomAttributes = { Helper.ExtensionAttribute }
            };
            @static.Methods.Add(method);

            var genericParameters = method.GenericParameters;

            var T = method.DefineUnmanagedGenericParameter();
            genericParameters.Add(T);

            var TAccumulate = new GenericParameter("TAccumulate", method);
            genericParameters.Add(TAccumulate);

            var TResult = new GenericParameter("TResult", method);
            genericParameters.Add(TResult);
            method.ReturnType = TResult;

            var Func = new GenericInstanceType(mainModule.GetType("UniNativeLinq", "RefAction`2"))
            {
                GenericArguments = { TAccumulate, T }
            };

            var ResultFunc = new GenericInstanceType(mainModule.GetType("UniNativeLinq", "RefFunc`2"))
            {
                GenericArguments = { TAccumulate, TResult }
            };

            if (isSpecial)
            {
                var (baseEnumerable, enumerable, enumerator) = T.MakeSpecialTypePair(name);
                switch (name)
                {
                    case "T[]":
                        GenerateArray(method, baseEnumerable, T, TAccumulate, Func, ResultFunc);
                        break;
                    case "NativeArray<T>":
                        GenerateNativeArray(method, baseEnumerable, enumerable, enumerator, T, TAccumulate, Func, ResultFunc);
                        break;
                    default: throw new NotSupportedException(name);
                }
            }
            else
            {
                GenerateNormal(method, Dictionary[name], T, TAccumulate, Func, ResultFunc);
            }
        }

        private static void GenerateArray(MethodDefinition method, TypeReference baseEnumerable, TypeReference T, TypeReference TAccumulate, TypeReference TFunc, TypeReference TResultFunc)
        {
            method.Parameters.Add(new ParameterDefinition("@this", ParameterAttributes.None, baseEnumerable));
            method.Parameters.Add(new ParameterDefinition("accumulate", ParameterAttributes.None, new ByReferenceType(TAccumulate)));
            method.Parameters.Add(new ParameterDefinition("func", ParameterAttributes.None, TFunc));
            method.Parameters.Add(new ParameterDefinition("resultFunc", ParameterAttributes.None, TResultFunc));

            var loopStart = Instruction.Create(OpCodes.Ldarg_2);
            var condition = Instruction.Create(OpCodes.Ldloc_0);

            var body = method.Body;
            body.InitLocals = true;
            body.Variables.Add(new VariableDefinition(method.Module.TypeSystem.Int32));
            body.Variables.Add(new VariableDefinition(method.Module.TypeSystem.Boolean));

            body.GetILProcessor()
                .ArgumentNullCheck(0, 2, 3, Instruction.Create(OpCodes.Br_S, condition))
                .Add(loopStart)
                .LdArg(1)
                .LdArg(0)
                .LdLoc(0)
                .LdElemA(T)
                .CallVirtual(TFunc.FindMethod("Invoke"))

                .LdLoc(0)
                .LdC(1)
                .Add()
                .StLoc(0)

                .Add(condition)
                .LdArg(0)
                .LdLen()
                .ConvI4()
                .BltS(loopStart)

                .LdArg(3)
                .LdArg(1)
                .CallVirtual(TResultFunc.FindMethod("Invoke"))
                .Ret();
        }

        private static void GenerateNativeArray(MethodDefinition method, TypeReference baseEnumerable, TypeReference enumerable, TypeReference enumerator, TypeReference T, TypeReference TAccumulate, TypeReference TFunc, TypeReference TResultFunc)
        {
            method.Parameters.Add(new ParameterDefinition("@this", ParameterAttributes.None, baseEnumerable));
            method.Parameters.Add(new ParameterDefinition("accumulate", ParameterAttributes.None, new ByReferenceType(TAccumulate)));
            method.Parameters.Add(new ParameterDefinition("func", ParameterAttributes.None, TFunc));
            method.Parameters.Add(new ParameterDefinition("resultFunc", ParameterAttributes.None, TResultFunc));

            var body = method.Body;

            var enumeratorVariable = new VariableDefinition(enumerator);
            var variables = body.Variables;
            variables.Add(enumeratorVariable);
            variables.Add(new VariableDefinition(method.Module.TypeSystem.Boolean));
            variables.Add(new VariableDefinition(new ByReferenceType(T)));
            variables.Add(new VariableDefinition(enumerable));

            var loopStart = Instruction.Create(OpCodes.Ldarg_2);
            var condition = Instruction.Create(OpCodes.Ldloca_S, enumeratorVariable);

            body.GetILProcessor()
                .ArgumentNullCheck(2, 3, Instruction.Create(OpCodes.Ldloca_S, variables[3]))
                .Dup()
                .LdArg(0)
                .Call(enumerable.FindMethod(".ctor", 1))
                .Call(enumerable.FindMethod("GetEnumerator", 0))
                .StLoc(0)
                .BrS(condition)
                .Add(loopStart)
                .LdArg(1)
                .LdLoc(2)
                .CallVirtual(TFunc.FindMethod("Invoke"))
                .Add(condition)
                .LdLocA(1)
                .Call(enumerator.FindMethod("TryGetNext"))
                .StLoc(2)
                .LdLoc(1)
                .BrTrueS(loopStart)
                .LdArg(3)
                .LdArg(1)
                .CallVirtual(TResultFunc.FindMethod("Invoke"))
                .Ret();
        }

        private static void GenerateGeneric(TypeDefinition @static, ModuleDefinition mainModule)
        {
            var method = new MethodDefinition("Aggregate", Helper.StaticMethodAttributes, mainModule.TypeSystem.Boolean)
            {
                DeclaringType = @static,
                AggressiveInlining = true,
                CustomAttributes = { Helper.ExtensionAttribute }
            };
            @static.Methods.Add(method);

            var genericParameters = method.GenericParameters;

            var (T, TEnumerator, TEnumerable) = method.Define3GenericParameters();

            var TAccumulate = new GenericParameter("TAccumulate", method);
            genericParameters.Add(TAccumulate);

            var TResult = new GenericParameter("TResult", method);
            genericParameters.Add(TResult);
            method.ReturnType = TResult;

            var TFunc = new GenericInstanceType(mainModule.GetType("UniNativeLinq", "RefAction`2"))
            {
                GenericArguments = { TAccumulate, T }
            };

            var TResultFunc = new GenericInstanceType(mainModule.GetType("UniNativeLinq", "RefFunc`2"))
            {
                GenericArguments = { TAccumulate, TResult }
            };

            method.Parameters.Add(new ParameterDefinition("@this", ParameterAttributes.In, new ByReferenceType(TEnumerable))
            {
                CustomAttributes = { Helper.GetSystemRuntimeCompilerServicesIsReadOnlyAttributeTypeReference() }
            });
            method.Parameters.Add(new ParameterDefinition("accumulate", ParameterAttributes.None, new ByReferenceType(TAccumulate)));
            method.Parameters.Add(new ParameterDefinition("func", ParameterAttributes.None, TFunc));
            method.Parameters.Add(new ParameterDefinition("resultFunc", ParameterAttributes.None, TResultFunc));

            var body = method.Body;

            var enumeratorVariable = new VariableDefinition(TEnumerator);
            body.Variables.Add(enumeratorVariable);
            body.Variables.Add(new VariableDefinition(method.Module.TypeSystem.Boolean));
            body.Variables.Add(new VariableDefinition(new ByReferenceType(T)));

            var loopStart = Instruction.Create(OpCodes.Ldarg_2);
            var condition = Instruction.Create(OpCodes.Ldloca_S, enumeratorVariable);

            body.GetILProcessor()
                .ArgumentNullCheck(2, 3, Instruction.Create(OpCodes.Ldarg_0))
                .GetEnumeratorEnumerable(TEnumerable)
                .StLoc(0)
                .BrS(condition)
                .Add(loopStart)
                .LdArg(1)
                .LdLoc(2)
                .CallVirtual(TFunc.FindMethod("Invoke"))
                .Add(condition)
                .LdLocA(1)
                .TryGetNextEnumerator(TEnumerator)
                .StLoc(2)
                .LdLoc(1)
                .BrTrueS(loopStart)
                .LdLocA(0)
                .DisposeEnumerator(TEnumerator)
                .LdArg(3)
                .LdArg(1)
                .CallVirtual(TResultFunc.FindMethod("Invoke"))
                .Ret();
        }

        private static void GenerateNormal(MethodDefinition method, TypeDefinition type, TypeReference T, TypeReference TAccumulate, TypeReference TFunc, TypeReference TResultFunc)
        {
            var (enumerable, enumerator, _) = T.MakeFromCommonType(method, type, "0");
            method.Parameters.Add(new ParameterDefinition("@this", ParameterAttributes.In, new ByReferenceType(enumerable))
            {
                CustomAttributes = { Helper.GetSystemRuntimeCompilerServicesIsReadOnlyAttributeTypeReference() }
            });
            method.Parameters.Add(new ParameterDefinition("accumulate", ParameterAttributes.None, new ByReferenceType(TAccumulate)));
            method.Parameters.Add(new ParameterDefinition("func", ParameterAttributes.None, TFunc));
            method.Parameters.Add(new ParameterDefinition("resultFunc", ParameterAttributes.None, TResultFunc));

            var body = method.Body;

            var enumeratorVariable = new VariableDefinition(enumerator);
            body.Variables.Add(enumeratorVariable);
            body.Variables.Add(new VariableDefinition(method.Module.TypeSystem.Boolean));
            body.Variables.Add(new VariableDefinition(new ByReferenceType(T)));

            var loopStart = Instruction.Create(OpCodes.Ldarg_2);
            var condition = Instruction.Create(OpCodes.Ldloca_S, enumeratorVariable);

            body.GetILProcessor()
                .ArgumentNullCheck(2, 3, Instruction.Create(OpCodes.Ldarg_0))

                .Call(enumerable.FindMethod("GetEnumerator", 0))
                .StLoc(0)
                .BrS(condition)
                .Add(loopStart)
                .LdArg(1)
                .LdLoc(2)
                .CallVirtual(TFunc.FindMethod("Invoke"))
                .Add(condition)
                .LdLocA(1)
                .Call(enumerator.FindMethod("TryGetNext"))
                .StLoc(2)
                .LdLoc(1)
                .BrTrueS(loopStart)
                .LdLocA(0)
                .Call(enumerator.FindMethod("Dispose", 0))
                .LdArg(3)
                .LdArg(1)
                .CallVirtual(TResultFunc.FindMethod("Invoke"))
                .Ret();
        }
    }
}