 import { Directive, ElementRef, HostListener, Input, input, OnChanges, SimpleChanges } from '@angular/core';

@Directive({
  selector: '[appHightlight]',
})
export class Hightlight implements OnChanges {

@Input() externalvariable:string='black';
@Input('appHightlightCard') defaultColor :string='red';

  constructor(private ele:ElementRef) {
    this.ele.nativeElement.style.backgroundColor='gray';
   }
  ngOnChanges(): void {
    
  }


   @HostListener('mouseover') over()
   {
    this.ele.nativeElement.style.backgroundColor= this.externalvariable;

   }
   @HostListener('mouseout') out()
   {
    this.ele.nativeElement.style.backgroundColor='red';

   }
     
}
