module {
  llvm.func @t(%arg0: i64) -> i64 {
    %0 = llvm.bitcast %arg0 : i64 to f64
    %1 = arith.truncf %0 : f64 to f8E4M3FN
    %2 = arith.extf %1 : f8E4M3FN to f64
    %3 = arith.truncf %0 : f64 to f8E5M2
    %4 = arith.extf %3 : f8E5M2 to f64
    %5 = llvm.fptrunc %0 : f64 to f16
    %6 = llvm.fpext %5 : f16 to f64
    %7 = llvm.fptrunc %0 : f64 to bf16
    %8 = llvm.fpext %7 : bf16 to f64
    %9 = llvm.fadd %2, %4 : f64
    %10 = llvm.fadd %9, %6 : f64
    %11 = llvm.fadd %10, %8 : f64
    %12 = llvm.bitcast %11 : f64 to i64
    llvm.return %12 : i64
  }
}

