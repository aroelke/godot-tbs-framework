#!/usr/bin/perl
################################################################################
#
# createImage
#
# This script autogenerates many combinations of images into a tile patern for
# use in displaying a field. Each image is assigned a number based on the edges
# present.
#
# The bit values (X = current tile):
# 1 2 3
# 8 X 4
# 7 6 5
#
# Example: Image with number 0b11111000 (248) has all of its neigbors but the 
# top three. So this image looks like a flat border on top with no border
# on any other edge.
#
################################################################################

use GD;
use File::Path;




sub save
{
   my $image = shift;
   my $filename = shift;
   
   open(my $fh, ">", $filename) or die "Cannot Open \"$filename\" because: $!";
   binmode $fh;
   print $fh $image->png;
   
   close($fh);
}

sub blit
{
   my $dest     = shift;
   my $src      = shift;
   my $rotation = shift;
   my $dspImg   = $src;
   
   if($rotation == 1)
   {   
      $dspImg = $src->copyRotate90();
   }
   elsif($rotation == 2)
   {
      $dspImg = $src->copyRotate180();
   }
   elsif($rotation == 3)
   {
      $dspImg = $src->copyRotate270();
   }
   $dest->copy($dspImg, 0, 0, 0, 0, $src->width, $src->height);
   
}


#==============================================================================#
#=                                 BUILD PARTS                                =#
#==============================================================================#


my $partsDir = "parts";

mkpath($partsDir);

#======================================#
#=           CornerBlank              =#
#======================================#
# create a new image
$im = new GD::Image(32, 32);
$im->interlaced('true');

# allocate some colors
$background = $im->colorAllocateAlpha(255, 255, 255, 127);
$white1     = $im->colorAllocateAlpha(255, 255, 255, 0);
$white2     = $im->colorAllocateAlpha(255, 255, 255, 25);
$white3     = $im->colorAllocateAlpha(255, 255, 255, 50);
$black      = $im->colorAllocateAlpha(0,   0,   0,   0);
$grey       = $im->colorAllocateAlpha(127, 127, 127, 0);


$im->filledRectangle(0, 0, 3, 3, $white3); 

save($im, "$partsDir/corner_blank.png");

#======================================#
#=          CornerInside              =#
#======================================#
# create a new image
$im = new GD::Image(32, 32);
$im->interlaced('true');

# allocate some colors
$background = $im->colorAllocateAlpha(255, 255, 255, 127);
$white1     = $im->colorAllocateAlpha(255, 255, 255, 0);
$white2     = $im->colorAllocateAlpha(255, 255, 255, 25);
$white3     = $im->colorAllocateAlpha(255, 255, 255, 50);
$black      = $im->colorAllocateAlpha(0,   0,   0,   0);
$grey       = $im->colorAllocateAlpha(127, 127, 127, 0);


$im->line(0, 0, 3, 0, $black);
$im->line(1, 1, 3, 1, $grey);
$im->line(2, 2, 3, 2, $white1);
$im->line(3, 3, 3, 3, $white2);

$im->line(0, 1, 0, 3, $black);
$im->line(1, 2, 1, 3, $grey);
$im->line(2, 3, 2, 3, $white1);




save($im, "$partsDir/corner_inside.png");

#======================================#
#=            CornerHori              =#
#======================================#
# create a new image
$im = new GD::Image(32, 32);
$im->interlaced('true');

# allocate some colors
$background = $im->colorAllocateAlpha(255, 255, 255, 127);
$white1     = $im->colorAllocateAlpha(255, 255, 255, 0);
$white2     = $im->colorAllocateAlpha(255, 255, 255, 25);
$white3     = $im->colorAllocateAlpha(255, 255, 255, 50);
$black      = $im->colorAllocateAlpha(0,   0,   0,   0);
$grey       = $im->colorAllocateAlpha(127, 127, 127, 0);


$im->line(0, 0, 3, 0, $black);
$im->line(0, 1, 3, 1, $grey);
$im->line(0, 2, 3, 2, $white1);
$im->line(0, 3, 3, 3, $white2);


save($im, "$partsDir/corner_hori.png");


#======================================#
#=            CornerVert              =#
#======================================#
# create a new image
$im = new GD::Image(32, 32);
$im->interlaced('true');

# allocate some colors
$background = $im->colorAllocateAlpha(255, 255, 255, 127);
$white1     = $im->colorAllocateAlpha(255, 255, 255, 0);
$white2     = $im->colorAllocateAlpha(255, 255, 255, 25);
$white3     = $im->colorAllocateAlpha(255, 255, 255, 50);
$black      = $im->colorAllocateAlpha(0,   0,   0,   0);
$grey       = $im->colorAllocateAlpha(127, 127, 127, 0);


$im->line(0, 0, 0, 3, $black);
$im->line(1, 0, 1, 3, $grey);
$im->line(2, 0, 2, 3, $white1);
$im->line(3, 0, 3, 3, $white2);


save($im, "$partsDir/corner_vert.png");


#======================================#
#=            CornerOutside           =#
#======================================#
# create a new image
$im = new GD::Image(32, 32);
$im->interlaced('true');

# allocate some colors
$background = $im->colorAllocateAlpha(255, 255, 255, 127);
$white1     = $im->colorAllocateAlpha(255, 255, 255, 0);
$white2     = $im->colorAllocateAlpha(255, 255, 255, 25);
$white3     = $im->colorAllocateAlpha(255, 255, 255, 50);
$black      = $im->colorAllocateAlpha(0,   0,   0,   0);
$grey       = $im->colorAllocateAlpha(127, 127, 127, 0);


$im->line(0, 0, 0, 0, $black);
$im->line(1, 1, 1, 0, $grey);
$im->line(2, 2, 2, 0, $white1);
$im->line(3, 3, 3, 0, $white2);

$im->line(0, 1, 0, 1, $grey);
$im->line(1, 2, 0, 2, $white1);
$im->line(2, 3, 0, 3, $white2);


save($im, "$partsDir/corner_outside.png");



#======================================#
#=             TopBlank               =#
#======================================#
# create a new image
$im = new GD::Image(32, 32);
$im->interlaced('true');

# allocate some colors
$background = $im->colorAllocateAlpha(255, 255, 255, 127);
$white1     = $im->colorAllocateAlpha(255, 255, 255, 0);
$white2     = $im->colorAllocateAlpha(255, 255, 255, 25);
$white3     = $im->colorAllocateAlpha(255, 255, 255, 50);
$black      = $im->colorAllocateAlpha(0,   0,   0,   0);
$grey       = $im->colorAllocateAlpha(127, 127, 127, 0);

$im->filledRectangle(4, 0, 27, 3, $white3);



save($im, "$partsDir/top_blank.png");


#======================================#
#=             TopFill                =#
#======================================#
# create a new image
$im = new GD::Image(32, 32);
$im->interlaced('true');

# allocate some colors
$background = $im->colorAllocateAlpha(255, 255, 255, 127);
$white1     = $im->colorAllocateAlpha(255, 255, 255, 0);
$white2     = $im->colorAllocateAlpha(255, 255, 255, 25);
$white3     = $im->colorAllocateAlpha(255, 255, 255, 50);
$black      = $im->colorAllocateAlpha(0,   0,   0,   0);
$grey       = $im->colorAllocateAlpha(127, 127, 127, 0);



$im->line(4, 0, 27, 0, $black);
$im->line(4, 1, 27, 1, $grey);
$im->line(4, 2, 27, 2, $white1);
$im->line(4, 3, 27, 3, $white2);



save($im, "$partsDir/top_fill.png");

#======================================#
#=             CenterBlank            =#
#======================================#
# create a new image
$im = new GD::Image(32, 32);
$im->interlaced('true');

# allocate some colors
$background = $im->colorAllocateAlpha(255, 255, 255, 127);
$white1     = $im->colorAllocateAlpha(255, 255, 255, 0);
$white2     = $im->colorAllocateAlpha(255, 255, 255, 25);
$white3     = $im->colorAllocateAlpha(255, 255, 255, 50);
$black      = $im->colorAllocateAlpha(0,   0,   0,   0);
$grey       = $im->colorAllocateAlpha(127, 127, 127, 0);


$im->filledRectangle(4, 4, 27, 27, $white3);

save($im, "$partsDir/center_blank.png");

#==============================================================================#
#=                                BUILD TILES                                 =#
#==============================================================================#

$tileDir = "tiles";
mkpath($tileDir);


$centerBlank   = new GD::Image("$partsDir/center_blank.png");
$cornerInside  = new GD::Image("$partsDir/corner_inside.png");
$cornerOutside = new GD::Image("$partsDir/corner_outside.png");
$topFill       = new GD::Image("$partsDir/top_fill.png");
$topBlank      = new GD::Image("$partsDir/top_blank.png");
$cornerHori    = new GD::Image("$partsDir/corner_hori.png");
$cornerVert    = new GD::Image("$partsDir/corner_vert.png");
$cornerBlank   = new GD::Image("$partsDir/corner_blank.png");

%cornerTable = 
(
   0 => $cornerInside,
   1 => $cornerHori,
   2 => $cornerInside,
   3 => $cornerHori,
   4 => $cornerVert,
   5 => $cornerOutside,
   6 => $cornerVert,
   7 => $cornerBlank
);

%sideTable =
(
   0 => $topFill,
   1 => $topFill,
   2 => $topFill,
   3 => $topFill,
   4 => $topBlank,
   5 => $topBlank,
   6 => $topBlank,
   7 => $topBlank
);

for($count = 0; $count < 256; $count++)
{
   


   $im = new GD::Image(32, 32);
   $im->interlaced('true');

   blit($im, $centerBlank);
   
   $prevSideBit = 1 << 7;
   
   for($side = 0; $side < 4; $side++)
   {
      $cornerBit = 1 << ($side * 2);
      $sideBit   = 1 << (($side * 2) + 1);
      
      $data = 0;
      
      if($prevSideBit & $count)
      {
         $data = $data | 1;
      }
      if($cornerBit & $count)
      {
         $data = $data | 2;   
      }
      if($sideBit & $count)
      {
         $data = $data | 4;
      }
      
      blit($im, $sideTable{$data},   $side);
      blit($im, $cornerTable{$data}, $side);

      
      
      $prevSideBit = $sideBit;
   }
   
   $filename = sprintf("%s/tile_%03d.png", $tileDir, $count);
   
   save($im, $filename);


}

#==============================================================================#
#=                                BUILD MONTAGE                               =#
#==============================================================================#

$im = new GD::Image(512, 512);
$im->interlaced('true');

$x = 0;
$xc = 0;
$yc = 0;

for($count = 0; $count < 256; $count++)
{
   $filename = sprintf("%s/tile_%03d.png", $tileDir, $count);
   $tile = new GD::Image($filename);
   
   $im->copy($tile, $xc, $yc, 0, 0, $tile->width, $tile->height);

   $x = $x + 1;
   $xc = $xc + 32;
   if($x >= 16)
   {
      $x = 0;
      $xc = 0;
      $yc = $yc + 32;
   }

}

save($im, "field.png");

