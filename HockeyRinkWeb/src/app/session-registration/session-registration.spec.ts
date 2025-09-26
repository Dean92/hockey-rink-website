import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SessionRegistration } from './session-registration';

describe('SessionRegistration', () => {
  let component: SessionRegistration;
  let fixture: ComponentFixture<SessionRegistration>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SessionRegistration]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SessionRegistration);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
