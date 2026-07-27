import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Confirmemail } from './confirmemail';

describe('Confirmemail', () => {
  let component: Confirmemail;
  let fixture: ComponentFixture<Confirmemail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Confirmemail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Confirmemail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
