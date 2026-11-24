namespace DxfToRevit.Core.Import;

/// <summary>Requests split into those to create and those already in the model.</summary>
/// <typeparam name="T">Request type.</typeparam>
/// <param name="New">Requests with no matching element in the model.</param>
/// <param name="Existing">Request id → id of the element that is already in the model.</param>
public sealed record ExistingSplit<T>(IReadOnlyList<T> New, IReadOnlyDictionary<string, string> Existing);
