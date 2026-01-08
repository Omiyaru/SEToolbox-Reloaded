// using System;
// using System.Collections.Generic;
// using System.Collections.Immutable;
// using System.Linq;
// using System.Threading.Tasks;

// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Microsoft.CodeAnalysis.Diagnostics;

// namespace CSharpScriptValidator
// {
//     public class ScriptValidator
//     {
//         private readonly LanguageVersion _languageVersion;

//         /// <summary>
//         /// Creates a new validator targeting the specified C# language version.
//         /// Default is C# 7.3.
//         /// </summary>
//         public ScriptValidator(string languageVersion = "7.3")
//         {
//             _languageVersion = ParseLanguageVersion(languageVersion);
//         }

//         /// <summary>
//         /// Validates the given C# script source code for compatibility with the set C# language version.
//         /// Returns a list of errors found. If empty, the script is valid.
//         /// </summary>
//         public async Task<List<string>> ValidateAsync(string scriptSource)
//         {
//             if (string.IsNullOrWhiteSpace(scriptSource))
//                 throw new ArgumentNullException(nameof(scriptSource));

//             var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(_languageVersion);
//             var syntaxTree = CSharpSyntaxTree.ParseText(scriptSource, parseOptions);

//             var compilation = CSharpCompilation.Create("temp", new[] { syntaxTree });

//             var analyzers = new List<DiagnosticAnalyzer>
//             {
//                 new NullReferenceAnalyzer(),
//                 new UnhandledExceptionAnalyzer()
//             };

//             var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

//             var errors = diagnostics
//                 .Where(d => d.event == DiagnosticSeverity.Error)
//                 .Select(FormatDiagnostic)
//                 .ToList();

//             return errors;
//         }

//         private static string FormatDiagnostic(Diagnostic diag)
//         {
//             var location = diag.Location;
//             var lineSpan = location.GetLineSpan();
//             var line = lineSpan.StartLinePosition.Line + 1; // zero-based
//             var col = lineSpan.StartLinePosition.Character + 1;

//             return $"Error: {diag.GetMessage()} (Line {line}, Col {col})";
//         }

//         private static LanguageVersion ParseLanguageVersion(string version)
//         {
//             if (string.IsNullOrWhiteSpace(version))
//                 throw new ArgumentNullException(nameof(version));

//             version = version.Trim().ToLowerInvariant();

//             return version switch
//             {
//                 "3" or "3.0" => LanguageVersion.CSharp3,
//                 "4" or "4.0" => LanguageVersion.CSharp4,
//                 "5" or "5.0" => LanguageVersion.CSharp5,
//                 "6" or "6.0" => LanguageVersion.CSharp6,
//                 "7" or "7.0" => LanguageVersion.CSharp7,
//                 "7.1" => LanguageVersion.CSharp7_1,
//                 "7.2" => LanguageVersion.CSharp7_2,
//                 "7.3" => LanguageVersion.CSharp7_3,
//                 "8" or "8.0" => LanguageVersion.CSharp8,
//                 "9" or "9.0" => LanguageVersion.CSharp9,
//                 "10" or "10.0" => LanguageVersion.CSharp10,
//                 "11" or "11.0" => LanguageVersion.CSharp11,
//                 "latest" => LanguageVersion.Latest,
//                 "preview" => LanguageVersion.Preview,
//                 _ => throw new ArgumentException($"Unsupported C# version: '{version}'"),
//             };
//         }
//     }

//     [DiagnosticAnalyzer(LanguageNames.CSharp)]
//     public class NullReferenceAnalyzer : DiagnosticAnalyzer
//     {
//         private static readonly DiagnosticDescriptor Rule = new(
//             "NullReferenceAnalyzer",
//             "Null Reference Exception",
//             "Possible null reference in method call",
//             "Usage",
//             DiagnosticSeverity.Error,
//             isEnabledByDefault: true);

//         public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

//         public override void Initialize(AnalysisContext context)
//         {
//             context.EnableConcurrentExecution();
//             context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
//             context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
//         }

//         private void AnalyzeNode(SyntaxNodeAnalysisContext context)
//         {
//             var invocation = (InvocationExpressionSyntax)context.Node;
//             var argument = invocation.ArgumentList.Arguments.FirstOrDefault();

//             if (argument == null)
//                 return;

//             var argumentType = context.SemanticModel.GetTypeInfo(argument.Expression).Type;
//             if (argumentType is null)
//                 return;

//             if (argumentType.Name == "object")
//             {
//                 var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
//                 context.ReportDiagnostic(diagnostic);
//             }
//         }
//     }

//     [DiagnosticAnalyzer(LanguageNames.CSharp)]
//     public class UnhandledExceptionAnalyzer : DiagnosticAnalyzer
//     {
//         private static readonly DiagnosticDescriptor Rule = new(
//             "UnhandledExceptionAnalyzer",
//             "Unhandled Exception",
//             "Catch block found, ensure exceptions are properly handled",
//             "Usage",
//             DiagnosticSeverity.Error,
//             isEnabledByDefault: true);

//         public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

//         public override void Initialize(AnalysisContext context)
//         {
//             context.EnableConcurrentExecution();
//             context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
//             context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CatchClause);
//         }

//         private void AnalyzeNode(SyntaxNodeAnalysisContext context)
//         {
//             var catchClause = (CatchClauseSyntax)context.Node;

//             if (catchClause.CatchKeyword.IsKind(SyntaxKind.CatchKeyword))
//             {
//                 var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
//                 context.ReportDiagnostic(diagnostic);
//             }
//         }
//     }
// }

