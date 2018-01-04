FILE(REMOVE_RECURSE
  "CMakeFiles/numcil"
  "bin/Debug/NumCIL.dll"
  "bin/Debug/NumCIL.Unsafe.dll"
  "NumCIL/TypedArrays.cs"
  "NumCIL/UFunc/TypedApplyUnary.cs"
  "NumCIL/UFunc/TypedApplyNullary.cs"
  "NumCIL/UFunc/TypedApplyBinary.cs"
  "NumCIL/UFunc/TypedReduce.cs"
  "NumCIL/UFunc/TypedAggregate.cs"
  "NumCIL.Unsafe/Copy.cs"
  "NumCIL.Unsafe/Pinner.cs"
  "NumCIL.Unsafe/ApplyBinary.cs"
  "NumCIL.Unsafe/ApplyUnary.cs"
  "NumCIL.Unsafe/Reduce.cs"
  "NumCIL.Unsafe/Aggregate.cs"
  "NumCIL.Unsafe/ApplyNullary.cs"
  "NumCIL.Unsafe/CreateAccessor.cs"
)

# Per-language clean rules from dependency scanning.
FOREACH(lang)
  INCLUDE(CMakeFiles/numcil.dir/cmake_clean_${lang}.cmake OPTIONAL)
ENDFOREACH(lang)
