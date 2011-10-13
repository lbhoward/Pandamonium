GIT STUFF

Make sure your local copy is up-to-date.

**This is also the same if, for whatever reason your online repository changes and you need to fetch any changes to update your local copy.
Just change the username from GamepadDave to your own.**

git remote add {name} git://github.com/GamepadDave/Pandamonium.git 
git fetch {name}
git merge {name}/master

You should now have the most up-to-date version of the project on your machine.


Making changes, uploading them and sending a pull request.

Save all changes on your local machine
(If you've only changed one file)
git add folder/subfolder/filename.extension
git commit -m 'Useful text which reflects the changes'
git remote add {name} git@github.com:USERNAME/Pandamonium.git
git push {name} master

In your browser, navigate to your github repository and click Pull Request. Follow the instructions.

I will then merge the changes in the request (If they look awesome) and both copies will be the same.


GAME STUFF
Controls:
A - Jump
Right Trigger - Shoot (this is currently limited to the direction the player is facing)
Left stick - Move