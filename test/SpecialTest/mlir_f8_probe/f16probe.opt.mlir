module {
  llvm.func @t(%arg0: i64) -> i64 {
    %0 = llvm.bitcast %arg0 : i64 to f64
    %1 = llvm.fptrunc %0 : f64 to f16
    %2 = llvm.fpext %1 : f16 to f64
    %3 = llvm.fptrunc %0 : f64 to bf16
    %4 = llvm.fpext %3 : bf16 to f64
    %5 = llvm.fadd %2, %4 : f64
    %6 = llvm.bitcast %5 : f64 to i64
    llvm.return %6 : i64
  }
}

