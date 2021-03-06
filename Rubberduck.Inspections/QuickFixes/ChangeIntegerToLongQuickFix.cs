﻿using System;
using Rubberduck.Parsing.Grammar;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Rubberduck.Common;
using Rubberduck.Inspections.Concrete;
using Rubberduck.Parsing.Inspections.Abstract;
using Rubberduck.Parsing.Inspections.Resources;
using Rubberduck.Parsing.Rewriter;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;

namespace Rubberduck.Inspections.QuickFixes
{
    public class ChangeIntegerToLongQuickFix : IQuickFix
    {
        private readonly RubberduckParserState _state;

        private static readonly HashSet<Type> _supportedInspections = new HashSet<Type>
        {
            typeof(IntegerDataTypeInspection)
        };

        public ChangeIntegerToLongQuickFix(RubberduckParserState state)
        {
            _state = state;
        }

        public IReadOnlyCollection<Type> SupportedInspections { get; } = _supportedInspections.ToList();

        public void Fix(IInspectionResult result)
        {
            var rewriter = _state.GetRewriter(result.Target);

            if (result.Target.HasTypeHint)
            {
                ReplaceTypeHint(result.Context, rewriter);
            }
            else
            {
                switch (result.Target.DeclarationType)
                {
                    case DeclarationType.Variable:
                        var variableContext = (VBAParser.VariableSubStmtContext)result.Context;
                        rewriter.Replace(variableContext.asTypeClause().type(), Tokens.Long);
                        break;
                    case DeclarationType.Constant:
                        var constantContext = (VBAParser.ConstSubStmtContext)result.Context;
                        rewriter.Replace(constantContext.asTypeClause().type(), Tokens.Long);
                        break;
                    case DeclarationType.Parameter:
                        var parameterContext = (VBAParser.ArgContext)result.Context;
                        rewriter.Replace(parameterContext.asTypeClause().type(), Tokens.Long);
                        break;
                    case DeclarationType.Function:
                        var functionContext = (VBAParser.FunctionStmtContext)result.Context;
                        rewriter.Replace(functionContext.asTypeClause().type(), Tokens.Long);
                        break;
                    case DeclarationType.PropertyGet:
                        var propertyContext = (VBAParser.PropertyGetStmtContext)result.Context;
                        rewriter.Replace(propertyContext.asTypeClause().type(), Tokens.Long);
                        break;
                    case DeclarationType.UserDefinedTypeMember:
                        var userDefinedTypeMemberContext = (VBAParser.UdtMemberContext)result.Context;
                        if (userDefinedTypeMemberContext.reservedNameMemberDeclaration() != null)
                        {
                            rewriter.Replace(
                                userDefinedTypeMemberContext.reservedNameMemberDeclaration().asTypeClause().type(),
                                Tokens.Long);

                        }
                        else
                        {
                            rewriter.Replace(
                                userDefinedTypeMemberContext.untypedNameMemberDeclaration().optionalArrayClause().asTypeClause().type(),
                                Tokens.Long);
                        }
                        break;
                }
            }

            var interfaceMembers = _state.DeclarationFinder.FindAllInterfaceMembers().ToArray();

            ParserRuleContext matchingInterfaceMemberContext;

            switch (result.Target.DeclarationType)
            {
                case DeclarationType.Parameter:
                    matchingInterfaceMemberContext =
                        interfaceMembers.Select(member => member.Context)
                            .FirstOrDefault(c => c == result.Context.Parent.Parent);

                    if (matchingInterfaceMemberContext != null)
                    {
                        var interfaceParameterIndex = GetParameterIndex((VBAParser.ArgContext)result.Context);

                        var implementationMembers =
                            _state.AllUserDeclarations.FindInterfaceImplementationMembers(interfaceMembers.First(
                                member => member.Context == matchingInterfaceMemberContext)).ToHashSet();

                        var parameterDeclarations =
                            _state.DeclarationFinder.UserDeclarations(DeclarationType.Parameter)
                                .Where(p => implementationMembers.Contains(p.ParentDeclaration))
                                .Cast<ParameterDeclaration>()
                                .ToArray();

                        foreach (var parameter in parameterDeclarations)
                        {
                            var parameterContext = (VBAParser.ArgContext)parameter.Context;
                            var parameterIndex = GetParameterIndex(parameterContext);

                            if (parameterIndex == interfaceParameterIndex)
                            {
                                var parameterRewriter = _state.GetRewriter(parameter);

                                if (parameter.HasTypeHint)
                                {
                                    ReplaceTypeHint(parameterContext, parameterRewriter);
                                }
                                else
                                {
                                    parameterRewriter.Replace(parameterContext.asTypeClause().type(), Tokens.Long);
                                }
                            }
                        }
                    }
                    break;
                case DeclarationType.Function:
                    matchingInterfaceMemberContext =
                        interfaceMembers.Select(member => member.Context)
                            .FirstOrDefault(c => c == result.Context);

                    if (matchingInterfaceMemberContext != null)
                    {
                        var functionDeclarations =
                            _state.AllUserDeclarations.FindInterfaceImplementationMembers(
                                    interfaceMembers.First(member => member.Context == matchingInterfaceMemberContext))
                                .Cast<FunctionDeclaration>()
                                .ToHashSet();

                        foreach (var function in functionDeclarations)
                        {
                            var functionRewriter = _state.GetRewriter(function);

                            if (function.HasTypeHint)
                            {
                                ReplaceTypeHint(function.Context, functionRewriter);
                            }
                            else
                            {
                                var functionContext = (VBAParser.FunctionStmtContext)function.Context;
                                functionRewriter.Replace(functionContext.asTypeClause().type(), Tokens.Long);
                            }
                        }
                    }
                    break;
                case DeclarationType.PropertyGet:
                    matchingInterfaceMemberContext =
                        interfaceMembers.Select(member => member.Context)
                            .FirstOrDefault(c => c == result.Context);

                    if (matchingInterfaceMemberContext != null)
                    {
                        var propertyGetDeclarations =
                            _state.AllUserDeclarations.FindInterfaceImplementationMembers(
                                    interfaceMembers.First(member => member.Context == matchingInterfaceMemberContext))
                                .Cast<PropertyGetDeclaration>()
                                .ToHashSet();

                        foreach (var propertyGet in propertyGetDeclarations)
                        {
                            var propertyGetRewriter = _state.GetRewriter(propertyGet);

                            if (propertyGet.HasTypeHint)
                            {
                                ReplaceTypeHint(propertyGet.Context, propertyGetRewriter);
                            }
                            else
                            {
                                var propertyGetContext = (VBAParser.PropertyGetStmtContext)propertyGet.Context;
                                propertyGetRewriter.Replace(propertyGetContext.asTypeClause().type(), Tokens.Long);
                            }
                        }
                    }
                    break;
            }
        }

        public string Description(IInspectionResult result)
        {
            return InspectionsUI.IntegerDataTypeQuickFix;
        }

        public bool CanFixInProcedure => true;
        public bool CanFixInModule => true;
        public bool CanFixInProject => true;

        private static int GetParameterIndex(VBAParser.ArgContext context)
        {
            return Array.IndexOf(((VBAParser.ArgListContext)context.Parent).arg().ToArray(), context);
        }

        private static void ReplaceTypeHint(RuleContext context, IModuleRewriter rewriter)
        {
            var typeHintContext = ParserRuleContextHelper.GetDescendent<VBAParser.TypeHintContext>(context);
            rewriter.Replace(typeHintContext, "&");
        }
    }
}
