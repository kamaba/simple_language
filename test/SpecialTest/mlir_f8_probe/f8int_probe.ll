; ModuleID = 'LLVMDialectModule'
source_filename = "LLVMDialectModule"

define i32 @e4m3_encode(float %0) {
  %2 = bitcast float %0 to i32
  %3 = lshr i32 %2, 31
  %4 = lshr i32 %2, 23
  %5 = and i32 %4, 255
  %6 = and i32 %2, 8388607
  %7 = sub i32 %5, 120
  %8 = shl i32 %3, 7
  %9 = or i32 %8, 127
  %10 = lshr i32 %6, 20
  %11 = and i32 %6, 1048575
  %12 = icmp ugt i32 %11, 524288
  %13 = icmp eq i32 %11, 524288
  %14 = and i32 %10, 1
  %15 = icmp ne i32 %14, 0
  %16 = and i1 %13, %15
  %17 = or i1 %12, %16
  %18 = add i32 %10, 1
  %19 = select i1 %17, i32 %18, i32 %10
  %20 = icmp eq i32 %19, 8
  %21 = add i32 %5, -119
  %22 = select i1 %20, i32 %21, i32 %7
  %23 = select i1 %20, i32 0, i32 %19
  %24 = shl i32 %22, 3
  %25 = or i32 %8, %24
  %26 = or i32 %25, %23
  %27 = icmp sgt i32 %22, 15
  %28 = select i1 %27, i32 %9, i32 %26
  %29 = icmp uge i32 %5, 1
  %30 = or i32 %6, 8388608
  %31 = select i1 %29, i32 %30, i32 %6
  %32 = select i1 %29, i32 21, i32 20
  %33 = sub i32 %32, %7
  %34 = icmp slt i32 %33, 1
  %35 = select i1 %34, i32 1, i32 %33
  %36 = icmp sgt i32 %35, 31
  %37 = select i1 %36, i32 31, i32 %35
  %38 = lshr i32 %31, %37
  %39 = shl i32 %38, %37
  %40 = sub i32 %31, %39
  %41 = sub i32 %37, 1
  %42 = shl i32 1, %41
  %43 = icmp ugt i32 %40, %42
  %44 = icmp eq i32 %40, %42
  %45 = and i32 %38, 1
  %46 = icmp ne i32 %45, 0
  %47 = and i1 %44, %46
  %48 = or i1 %43, %47
  %49 = add i32 %38, 1
  %50 = select i1 %48, i32 %49, i32 %38
  %51 = icmp eq i32 %50, 8
  %52 = or i32 %8, %50
  %53 = or i32 %8, 8
  %54 = select i1 %51, i32 %53, i32 %52
  %55 = icmp sgt i32 %7, 0
  %56 = select i1 %55, i32 %28, i32 %54
  %57 = icmp eq i32 %5, 255
  %58 = icmp sgt i32 %7, 15
  %59 = or i1 %57, %58
  %60 = select i1 %59, i32 %9, i32 %56
  ret i32 %60
}

define float @e4m3_decode(i32 %0) {
  %2 = lshr i32 %0, 7
  %3 = lshr i32 %0, 3
  %4 = and i32 %3, 15
  %5 = and i32 %0, 7
  %6 = shl i32 %2, 31
  %7 = icmp eq i32 %5, 0
  %8 = sitofp i32 %5 to float
  %9 = fmul float %8, 0x3F60000000000000
  %10 = bitcast float %9 to i32
  %11 = or i32 %6, %10
  %12 = select i1 %7, i32 %6, i32 %11
  %13 = icmp eq i32 %4, 15
  %14 = icmp eq i32 %5, 7
  %15 = and i1 %13, %14
  %16 = add i32 %4, 120
  %17 = shl i32 %16, 23
  %18 = shl i32 %5, 20
  %19 = or i32 %6, %17
  %20 = or i32 %19, %18
  %21 = select i1 %15, i32 2143289344, i32 %20
  %22 = icmp eq i32 %4, 0
  %23 = select i1 %22, i32 %12, i32 %21
  %24 = bitcast i32 %23 to float
  ret float %24
}

define i32 @e5m2_encode(float %0) {
  %2 = bitcast float %0 to i32
  %3 = lshr i32 %2, 31
  %4 = lshr i32 %2, 23
  %5 = and i32 %4, 255
  %6 = and i32 %2, 8388607
  %7 = sub i32 %5, 112
  %8 = shl i32 %3, 7
  %9 = or i32 %8, 124
  %10 = or i32 %8, 127
  %11 = icmp eq i32 %6, 0
  %12 = select i1 %11, i32 %9, i32 %10
  %13 = lshr i32 %6, 21
  %14 = and i32 %6, 2097151
  %15 = icmp ugt i32 %14, 1048576
  %16 = icmp eq i32 %14, 1048576
  %17 = and i32 %13, 1
  %18 = icmp ne i32 %17, 0
  %19 = and i1 %16, %18
  %20 = or i1 %15, %19
  %21 = add i32 %13, 1
  %22 = select i1 %20, i32 %21, i32 %13
  %23 = icmp eq i32 %22, 4
  %24 = add i32 %5, -111
  %25 = select i1 %23, i32 %24, i32 %7
  %26 = select i1 %23, i32 0, i32 %22
  %27 = shl i32 %25, 2
  %28 = or i32 %8, %27
  %29 = or i32 %28, %26
  %30 = icmp sge i32 %25, 31
  %31 = select i1 %30, i32 %9, i32 %29
  %32 = icmp uge i32 %5, 1
  %33 = or i32 %6, 8388608
  %34 = select i1 %32, i32 %33, i32 %6
  %35 = select i1 %32, i32 22, i32 21
  %36 = sub i32 %35, %7
  %37 = icmp slt i32 %36, 1
  %38 = select i1 %37, i32 1, i32 %36
  %39 = icmp sgt i32 %38, 31
  %40 = select i1 %39, i32 31, i32 %38
  %41 = lshr i32 %34, %40
  %42 = shl i32 %41, %40
  %43 = sub i32 %34, %42
  %44 = sub i32 %40, 1
  %45 = shl i32 1, %44
  %46 = icmp ugt i32 %43, %45
  %47 = icmp eq i32 %43, %45
  %48 = and i32 %41, 1
  %49 = icmp ne i32 %48, 0
  %50 = and i1 %47, %49
  %51 = or i1 %46, %50
  %52 = add i32 %41, 1
  %53 = select i1 %51, i32 %52, i32 %41
  %54 = icmp eq i32 %53, 4
  %55 = or i32 %8, %53
  %56 = or i32 %8, 4
  %57 = select i1 %54, i32 %56, i32 %55
  %58 = icmp sgt i32 %7, 0
  %59 = select i1 %58, i32 %31, i32 %57
  %60 = icmp sge i32 %7, 31
  %61 = select i1 %60, i32 %9, i32 %59
  %62 = icmp eq i32 %5, 255
  %63 = select i1 %62, i32 %12, i32 %61
  ret i32 %63
}

define float @e5m2_decode(i32 %0) {
  %2 = lshr i32 %0, 7
  %3 = lshr i32 %0, 2
  %4 = and i32 %3, 31
  %5 = and i32 %0, 3
  %6 = shl i32 %2, 31
  %7 = icmp eq i32 %5, 0
  %8 = sitofp i32 %5 to float
  %9 = fmul float %8, 0x3EF0000000000000
  %10 = bitcast float %9 to i32
  %11 = or i32 %6, %10
  %12 = select i1 %7, i32 %6, i32 %11
  %13 = icmp eq i32 %4, 31
  %14 = icmp eq i32 %5, 0
  %15 = or i32 %6, 2139095040
  %16 = select i1 %14, i32 %15, i32 2143289344
  %17 = add i32 %4, 112
  %18 = shl i32 %17, 23
  %19 = shl i32 %5, 21
  %20 = or i32 %6, %18
  %21 = or i32 %20, %19
  %22 = icmp eq i32 %4, 0
  %23 = select i1 %13, i32 %16, i32 %21
  %24 = select i1 %22, i32 %12, i32 %23
  %25 = bitcast i32 %24 to float
  ret float %25
}

define i64 @e4m3_rt(double %0) {
  %2 = fptrunc double %0 to float
  %3 = call i32 @e4m3_encode(float %2)
  %4 = call float @e4m3_decode(i32 %3)
  %5 = fpext float %4 to double
  %6 = bitcast double %5 to i64
  ret i64 %6
}

define i64 @e5m2_rt(double %0) {
  %2 = fptrunc double %0 to float
  %3 = call i32 @e5m2_encode(float %2)
  %4 = call float @e5m2_decode(i32 %3)
  %5 = fpext float %4 to double
  %6 = bitcast double %5 to i64
  ret i64 %6
}

define i32 @f16_encode(float %0) {
  %2 = bitcast float %0 to i32
  %3 = lshr i32 %2, 31
  %4 = lshr i32 %2, 23
  %5 = and i32 %4, 255
  %6 = and i32 %2, 8388607
  %7 = sub i32 %5, 112
  %8 = shl i32 %3, 15
  %9 = or i32 %8, 31744
  %10 = or i32 %8, 32767
  %11 = icmp eq i32 %6, 0
  %12 = select i1 %11, i32 %9, i32 %10
  %13 = lshr i32 %6, 13
  %14 = and i32 %6, 8191
  %15 = icmp ugt i32 %14, 4096
  %16 = icmp eq i32 %14, 4096
  %17 = and i32 %13, 1
  %18 = icmp ne i32 %17, 0
  %19 = and i1 %16, %18
  %20 = or i1 %15, %19
  %21 = add i32 %13, 1
  %22 = select i1 %20, i32 %21, i32 %13
  %23 = icmp eq i32 %22, 1024
  %24 = add i32 %5, -111
  %25 = select i1 %23, i32 %24, i32 %7
  %26 = select i1 %23, i32 0, i32 %22
  %27 = shl i32 %25, 10
  %28 = or i32 %8, %27
  %29 = or i32 %28, %26
  %30 = icmp sge i32 %25, 31
  %31 = select i1 %30, i32 %9, i32 %29
  %32 = icmp uge i32 %5, 1
  %33 = or i32 %6, 8388608
  %34 = select i1 %32, i32 %33, i32 %6
  %35 = select i1 %32, i32 14, i32 13
  %36 = sub i32 %35, %7
  %37 = icmp slt i32 %36, 1
  %38 = select i1 %37, i32 1, i32 %36
  %39 = icmp sgt i32 %38, 31
  %40 = select i1 %39, i32 31, i32 %38
  %41 = lshr i32 %34, %40
  %42 = shl i32 %41, %40
  %43 = sub i32 %34, %42
  %44 = sub i32 %40, 1
  %45 = shl i32 1, %44
  %46 = icmp ugt i32 %43, %45
  %47 = icmp eq i32 %43, %45
  %48 = and i32 %41, 1
  %49 = icmp ne i32 %48, 0
  %50 = and i1 %47, %49
  %51 = or i1 %46, %50
  %52 = add i32 %41, 1
  %53 = select i1 %51, i32 %52, i32 %41
  %54 = icmp eq i32 %53, 1024
  %55 = or i32 %8, %53
  %56 = or i32 %8, 1024
  %57 = select i1 %54, i32 %56, i32 %55
  %58 = icmp sgt i32 %7, 0
  %59 = select i1 %58, i32 %31, i32 %57
  %60 = icmp sge i32 %7, 31
  %61 = select i1 %60, i32 %9, i32 %59
  %62 = icmp eq i32 %5, 255
  %63 = select i1 %62, i32 %12, i32 %61
  ret i32 %63
}

define float @f16_decode(i32 %0) {
  %2 = lshr i32 %0, 15
  %3 = lshr i32 %0, 10
  %4 = and i32 %3, 31
  %5 = and i32 %0, 1023
  %6 = shl i32 %2, 31
  %7 = icmp eq i32 %5, 0
  %8 = sitofp i32 %5 to float
  %9 = fmul float %8, 0x3E70000000000000
  %10 = bitcast float %9 to i32
  %11 = or i32 %6, %10
  %12 = select i1 %7, i32 %6, i32 %11
  %13 = icmp eq i32 %4, 31
  %14 = icmp eq i32 %5, 0
  %15 = or i32 %6, 2139095040
  %16 = select i1 %14, i32 %15, i32 2143289344
  %17 = add i32 %4, 112
  %18 = shl i32 %17, 23
  %19 = shl i32 %5, 13
  %20 = or i32 %6, %18
  %21 = or i32 %20, %19
  %22 = icmp eq i32 %4, 0
  %23 = select i1 %13, i32 %16, i32 %21
  %24 = select i1 %22, i32 %12, i32 %23
  %25 = bitcast i32 %24 to float
  ret float %25
}

define i32 @bf16_encode(float %0) {
  %2 = bitcast float %0 to i32
  %3 = lshr i32 %2, 31
  %4 = lshr i32 %2, 23
  %5 = and i32 %4, 255
  %6 = and i32 %2, 8388607
  %7 = shl i32 %3, 15
  %8 = or i32 %7, 32640
  %9 = or i32 %7, 32767
  %10 = icmp eq i32 %6, 0
  %11 = select i1 %10, i32 %8, i32 %9
  %12 = lshr i32 %6, 16
  %13 = and i32 %6, 65535
  %14 = icmp ugt i32 %13, 32768
  %15 = icmp eq i32 %13, 32768
  %16 = and i32 %12, 1
  %17 = icmp ne i32 %16, 0
  %18 = and i1 %15, %17
  %19 = or i1 %14, %18
  %20 = add i32 %12, 1
  %21 = select i1 %19, i32 %20, i32 %12
  %22 = icmp eq i32 %21, 128
  %23 = add i32 %5, 1
  %24 = select i1 %22, i32 %23, i32 %5
  %25 = select i1 %22, i32 0, i32 %21
  %26 = shl i32 %24, 7
  %27 = or i32 %7, %26
  %28 = or i32 %27, %25
  %29 = icmp sge i32 %24, 255
  %30 = select i1 %29, i32 %8, i32 %28
  %31 = icmp uge i32 %5, 1
  %32 = or i32 %6, 8388608
  %33 = select i1 %31, i32 %32, i32 %6
  %34 = select i1 %31, i32 17, i32 16
  %35 = sub i32 %34, %5
  %36 = icmp slt i32 %35, 1
  %37 = select i1 %36, i32 1, i32 %35
  %38 = icmp sgt i32 %37, 31
  %39 = select i1 %38, i32 31, i32 %37
  %40 = lshr i32 %33, %39
  %41 = shl i32 %40, %39
  %42 = sub i32 %33, %41
  %43 = sub i32 %39, 1
  %44 = shl i32 1, %43
  %45 = icmp ugt i32 %42, %44
  %46 = icmp eq i32 %42, %44
  %47 = and i32 %40, 1
  %48 = icmp ne i32 %47, 0
  %49 = and i1 %46, %48
  %50 = or i1 %45, %49
  %51 = add i32 %40, 1
  %52 = select i1 %50, i32 %51, i32 %40
  %53 = icmp eq i32 %52, 128
  %54 = or i32 %7, %52
  %55 = or i32 %7, 128
  %56 = select i1 %53, i32 %55, i32 %54
  %57 = icmp sgt i32 %5, 0
  %58 = select i1 %57, i32 %30, i32 %56
  %59 = icmp sge i32 %5, 255
  %60 = select i1 %59, i32 %8, i32 %58
  %61 = icmp eq i32 %5, 255
  %62 = select i1 %61, i32 %11, i32 %60
  ret i32 %62
}

define float @bf16_decode(i32 %0) {
  %2 = lshr i32 %0, 15
  %3 = lshr i32 %0, 7
  %4 = and i32 %3, 255
  %5 = and i32 %0, 127
  %6 = shl i32 %2, 31
  %7 = icmp eq i32 %5, 0
  %8 = shl i32 %5, 16
  %9 = or i32 %6, %8
  %10 = select i1 %7, i32 %6, i32 %9
  %11 = icmp eq i32 %4, 255
  %12 = icmp eq i32 %5, 0
  %13 = or i32 %6, 2139095040
  %14 = select i1 %12, i32 %13, i32 2143289344
  %15 = shl i32 %4, 23
  %16 = shl i32 %5, 16
  %17 = or i32 %6, %15
  %18 = or i32 %17, %16
  %19 = icmp eq i32 %4, 0
  %20 = select i1 %11, i32 %14, i32 %18
  %21 = select i1 %19, i32 %10, i32 %20
  %22 = bitcast i32 %21 to float
  ret float %22
}

define i64 @f16_rt(double %0) {
  %2 = fptrunc double %0 to float
  %3 = call i32 @f16_encode(float %2)
  %4 = call float @f16_decode(i32 %3)
  %5 = fpext float %4 to double
  %6 = bitcast double %5 to i64
  ret i64 %6
}

define i64 @bf16_rt(double %0) {
  %2 = fptrunc double %0 to float
  %3 = call i32 @bf16_encode(float %2)
  %4 = call float @bf16_decode(i32 %3)
  %5 = fpext float %4 to double
  %6 = bitcast double %5 to i64
  ret i64 %6
}

!llvm.module.flags = !{!0}

!0 = !{i32 2, !"Debug Info Version", i32 3}
