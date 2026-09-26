; ModuleID = 'LLVMDialectModule'
source_filename = "LLVMDialectModule"

define i64 @t(i64 %0) {
  %2 = bitcast i64 %0 to double
  %3 = fptrunc double %2 to half
  %4 = fpext half %3 to double
  %5 = fptrunc double %2 to bfloat
  %6 = fpext bfloat %5 to double
  %7 = fadd double %4, %6
  %8 = bitcast double %7 to i64
  ret i64 %8
}

!llvm.module.flags = !{!0}

!0 = !{i32 2, !"Debug Info Version", i32 3}
