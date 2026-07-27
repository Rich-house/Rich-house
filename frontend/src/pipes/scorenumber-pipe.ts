import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'scorenumber',
})
export class ScorenumberPipe implements PipeTransform {

  transform(value: number):number {
    
    return Math.pow(value,2);
  }

}
